using AscensoresIruna.Api.Models;
using AscensoresIruna.Api.Services;
using AscensoresIruna.Api.Tests.Helpers;

namespace AscensoresIruna.Api.Tests.Services;

public class ElevatorStatusServiceTests
{
    private static DateTimeOffset BaseUtcNow => new(2026, 6, 12, 12, 0, 0, TimeSpan.Zero);

    private static DateTime ToSpainTime(DateTimeOffset utc)
    {
        var spainTz = TimeZoneInfo.FindSystemTimeZoneById("Romance Standard Time");
        return TimeZoneInfo.ConvertTimeFromUtc(utc.UtcDateTime, spainTz);
    }

    [Fact]
    public async Task NoReports_ReturnsDesconocido()
    {
        using var context = TestDbHelper.CreateInMemoryContext();
        TestDbHelper.SeedElevator(context);
        var fakeTime = new FakeTimeProvider(BaseUtcNow);
        var service = new ElevatorStatusService(context, fakeTime);

        var result = await service.GetCurrentStatusAsync(1);

        Assert.Equal(ElevatorStatus.Desconocido, result.Status);
        Assert.Null(result.LastReportedAt);
        Assert.Equal(0, result.RecentReports);
    }

    [Fact]
    public async Task SingleFirstReport_Operativo_AcceptedDirectly()
    {
        using var context = TestDbHelper.CreateInMemoryContext();
        TestDbHelper.SeedElevator(context);
        var fakeTime = new FakeTimeProvider(BaseUtcNow);
        var now = ToSpainTime(BaseUtcNow);
        TestDbHelper.AddReport(context, 1, ElevatorStatus.Operativo, now.AddMinutes(-5), "ip1");
        var service = new ElevatorStatusService(context, fakeTime);

        var result = await service.GetCurrentStatusAsync(1);

        Assert.Equal(ElevatorStatus.Operativo, result.Status);
    }

    [Fact]
    public async Task SingleFirstReport_NoOperativo_AcceptedDirectly()
    {
        using var context = TestDbHelper.CreateInMemoryContext();
        TestDbHelper.SeedElevator(context);
        var fakeTime = new FakeTimeProvider(BaseUtcNow);
        var now = ToSpainTime(BaseUtcNow);
        TestDbHelper.AddReport(context, 1, ElevatorStatus.NoOperativo, now.AddMinutes(-5), "ip1");
        var service = new ElevatorStatusService(context, fakeTime);

        var result = await service.GetCurrentStatusAsync(1);

        Assert.Equal(ElevatorStatus.NoOperativo, result.Status);
    }

    [Fact]
    public async Task AllReports_SameStatus_ReturnsThatStatus()
    {
        using var context = TestDbHelper.CreateInMemoryContext();
        TestDbHelper.SeedElevator(context);
        var fakeTime = new FakeTimeProvider(BaseUtcNow);
        var now = ToSpainTime(BaseUtcNow);

        for (int i = 0; i < 5; i++)
            TestDbHelper.AddReport(context, 1, ElevatorStatus.Operativo, now.AddMinutes(-i * 5), $"ip{i}");

        TestDbHelper.AddReport(context, 1, ElevatorStatus.Operativo, now.AddHours(-3), "old_ip");

        var service = new ElevatorStatusService(context, fakeTime);
        var result = await service.GetCurrentStatusAsync(1);

        Assert.Equal(ElevatorStatus.Operativo, result.Status);
    }

    [Fact]
    public async Task ClearMajority_Operativo_Wins()
    {
        using var context = TestDbHelper.CreateInMemoryContext();
        TestDbHelper.SeedElevator(context);
        var fakeTime = new FakeTimeProvider(BaseUtcNow);
        var now = ToSpainTime(BaseUtcNow);

        TestDbHelper.AddReport(context, 1, ElevatorStatus.Operativo, now.AddHours(-3), "old");

        for (int i = 0; i < 4; i++)
            TestDbHelper.AddReport(context, 1, ElevatorStatus.Operativo, now.AddMinutes(-10), $"op{i}");
        TestDbHelper.AddReport(context, 1, ElevatorStatus.NoOperativo, now.AddMinutes(-10), "nop0");

        var service = new ElevatorStatusService(context, fakeTime);
        var result = await service.GetCurrentStatusAsync(1);

        Assert.Equal(ElevatorStatus.Operativo, result.Status);
    }

    [Fact]
    public async Task ClearMajority_NoOperativo_Wins()
    {
        using var context = TestDbHelper.CreateInMemoryContext();
        TestDbHelper.SeedElevator(context);
        var fakeTime = new FakeTimeProvider(BaseUtcNow);
        var now = ToSpainTime(BaseUtcNow);

        TestDbHelper.AddReport(context, 1, ElevatorStatus.Operativo, now.AddHours(-3), "old");

        for (int i = 0; i < 4; i++)
            TestDbHelper.AddReport(context, 1, ElevatorStatus.NoOperativo, now.AddMinutes(-10), $"nop{i}");
        TestDbHelper.AddReport(context, 1, ElevatorStatus.Operativo, now.AddMinutes(-10), "op0");

        var service = new ElevatorStatusService(context, fakeTime);
        var result = await service.GetCurrentStatusAsync(1);

        Assert.Equal(ElevatorStatus.NoOperativo, result.Status);
    }

    [Fact]
    public async Task CloseContest_60Percent_ReturnsParcial()
    {
        using var context = TestDbHelper.CreateInMemoryContext();
        TestDbHelper.SeedElevator(context);
        var fakeTime = new FakeTimeProvider(BaseUtcNow);
        var now = ToSpainTime(BaseUtcNow);

        TestDbHelper.AddReport(context, 1, ElevatorStatus.Operativo, now.AddHours(-3), "old");

        TestDbHelper.AddReport(context, 1, ElevatorStatus.Operativo, now.AddMinutes(-90), "ipA");
        TestDbHelper.AddReporter(context, "ipA", 5.0);

        TestDbHelper.AddReport(context, 1, ElevatorStatus.NoOperativo, now.AddMinutes(-90), "ipB");
        TestDbHelper.AddReporter(context, "ipB", 3.0);

        var service = new ElevatorStatusService(context, fakeTime);
        var result = await service.GetCurrentStatusAsync(1);

        Assert.Equal(ElevatorStatus.Parcial, result.Status);
    }

    [Fact]
    public async Task CloseContest_Below60Percent_ReturnsWinner()
    {
        using var context = TestDbHelper.CreateInMemoryContext();
        TestDbHelper.SeedElevator(context);
        var fakeTime = new FakeTimeProvider(BaseUtcNow);
        var now = ToSpainTime(BaseUtcNow);

        TestDbHelper.AddReport(context, 1, ElevatorStatus.Operativo, now.AddHours(-3), "old");

        TestDbHelper.AddReport(context, 1, ElevatorStatus.Operativo, now.AddMinutes(-90), "ipA");
        TestDbHelper.AddReporter(context, "ipA", 10.0);

        TestDbHelper.AddReport(context, 1, ElevatorStatus.NoOperativo, now.AddMinutes(-90), "ipB");
        TestDbHelper.AddReporter(context, "ipB", 5.9);

        var service = new ElevatorStatusService(context, fakeTime);
        var result = await service.GetCurrentStatusAsync(1);

        Assert.Equal(ElevatorStatus.Operativo, result.Status);
    }

    [Fact]
    public async Task TimeDecay_RecentReportsWeighMore()
    {
        using var context = TestDbHelper.CreateInMemoryContext();
        TestDbHelper.SeedElevator(context);
        var fakeTime = new FakeTimeProvider(BaseUtcNow);
        var now = ToSpainTime(BaseUtcNow);

        TestDbHelper.AddReport(context, 1, ElevatorStatus.Operativo, now.AddHours(-3), "old");

        TestDbHelper.AddReport(context, 1, ElevatorStatus.NoOperativo, now.AddMinutes(-5), "ip_recent1");
        TestDbHelper.AddReport(context, 1, ElevatorStatus.NoOperativo, now.AddMinutes(-10), "ip_recent2");

        TestDbHelper.AddReport(context, 1, ElevatorStatus.Operativo, now.AddMinutes(-90), "ip_old1");

        var service = new ElevatorStatusService(context, fakeTime);
        var result = await service.GetCurrentStatusAsync(1);

        Assert.Equal(ElevatorStatus.NoOperativo, result.Status);
    }

    [Fact]
    public async Task TimeDecay_20MinBoundary()
    {
        using var context = TestDbHelper.CreateInMemoryContext();
        TestDbHelper.SeedElevator(context);
        var fakeTime = new FakeTimeProvider(BaseUtcNow);
        var now = ToSpainTime(BaseUtcNow);

        TestDbHelper.AddReport(context, 1, ElevatorStatus.Operativo, now.AddHours(-3), "old");

        TestDbHelper.AddReport(context, 1, ElevatorStatus.Operativo, now.AddMinutes(-20), "ip_at20_a");
        TestDbHelper.AddReport(context, 1, ElevatorStatus.Operativo, now.AddMinutes(-20), "ip_at20_b");

        TestDbHelper.AddReport(context, 1, ElevatorStatus.NoOperativo, now.AddMinutes(-21), "ip_at21");

        var service = new ElevatorStatusService(context, fakeTime);
        var result = await service.GetCurrentStatusAsync(1);

        Assert.Equal(ElevatorStatus.Operativo, result.Status);
    }

    [Fact]
    public async Task TimeDecay_60MinBoundary()
    {
        using var context = TestDbHelper.CreateInMemoryContext();
        TestDbHelper.SeedElevator(context);
        var fakeTime = new FakeTimeProvider(BaseUtcNow);
        var now = ToSpainTime(BaseUtcNow);

        TestDbHelper.AddReport(context, 1, ElevatorStatus.Operativo, now.AddHours(-3), "old");

        TestDbHelper.AddReport(context, 1, ElevatorStatus.Operativo, now.AddMinutes(-60), "ip_at60");

        TestDbHelper.AddReport(context, 1, ElevatorStatus.NoOperativo, now.AddMinutes(-61), "ip_at61");

        var service = new ElevatorStatusService(context, fakeTime);
        var result = await service.GetCurrentStatusAsync(1);

        Assert.Equal(ElevatorStatus.Operativo, result.Status);
    }

    [Fact]
    public async Task TrustScore_HighTrust_OutweighsLowTrust()
    {
        using var context = TestDbHelper.CreateInMemoryContext();
        TestDbHelper.SeedElevator(context);
        var fakeTime = new FakeTimeProvider(BaseUtcNow);
        var now = ToSpainTime(BaseUtcNow);

        TestDbHelper.AddReport(context, 1, ElevatorStatus.Operativo, now.AddHours(-3), "old");

        TestDbHelper.AddReport(context, 1, ElevatorStatus.Operativo, now.AddMinutes(-5), "ip_trusted");
        TestDbHelper.AddReporter(context, "ip_trusted", 3.0);

        for (int i = 0; i < 3; i++)
        {
            var ipHash = $"ip_troll{i}";
            TestDbHelper.AddReport(context, 1, ElevatorStatus.NoOperativo, now.AddMinutes(-5), ipHash);
            TestDbHelper.AddReporter(context, ipHash, 0.1);
        }

        var service = new ElevatorStatusService(context, fakeTime);
        var result = await service.GetCurrentStatusAsync(1);

        Assert.Equal(ElevatorStatus.Operativo, result.Status);
    }

    [Fact]
    public async Task Reports_OlderThan2Hours_Ignored()
    {
        using var context = TestDbHelper.CreateInMemoryContext();
        TestDbHelper.SeedElevator(context);
        var fakeTime = new FakeTimeProvider(BaseUtcNow);
        var now = ToSpainTime(BaseUtcNow);

        TestDbHelper.AddReport(context, 1, ElevatorStatus.Operativo, now.AddHours(-3), "ip1");
        TestDbHelper.AddReport(context, 1, ElevatorStatus.NoOperativo, now.AddHours(-2.5), "ip2");

        var service = new ElevatorStatusService(context, fakeTime);
        var result = await service.GetCurrentStatusAsync(1);

        Assert.Equal(ElevatorStatus.Desconocido, result.Status);
        Assert.Equal(0, result.RecentReports);
    }

    [Fact]
    public async Task RecentReports_CountsOnlyLastHour()
    {
        using var context = TestDbHelper.CreateInMemoryContext();
        TestDbHelper.SeedElevator(context);
        var fakeTime = new FakeTimeProvider(BaseUtcNow);
        var now = ToSpainTime(BaseUtcNow);

        TestDbHelper.AddReport(context, 1, ElevatorStatus.Operativo, now.AddHours(-3), "old");

        TestDbHelper.AddReport(context, 1, ElevatorStatus.Operativo, now.AddMinutes(-30), "ip1");
        TestDbHelper.AddReport(context, 1, ElevatorStatus.Operativo, now.AddMinutes(-45), "ip2");
        TestDbHelper.AddReport(context, 1, ElevatorStatus.Operativo, now.AddMinutes(-90), "ip3");

        var service = new ElevatorStatusService(context, fakeTime);
        var result = await service.GetCurrentStatusAsync(1);

        Assert.Equal(2, result.RecentReports);
    }
}
