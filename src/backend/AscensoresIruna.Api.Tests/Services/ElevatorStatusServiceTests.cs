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
    public async Task NoReports_ReturnsDesconocido_NoConflict()
    {
        using var context = TestDbHelper.CreateInMemoryContext();
        TestDbHelper.SeedElevator(context);
        var fakeTime = new FakeTimeProvider(BaseUtcNow);
        var service = new ElevatorStatusService(context, fakeTime);

        var result = await service.GetCurrentStatusAsync(1);

        Assert.Equal(ElevatorStatus.Desconocido, result.Status);
        Assert.False(result.HasConflict);
        Assert.Null(result.LastReportedAt);
        Assert.Equal(0, result.RecentReports);
    }

    [Fact]
    public async Task SingleReport_Operativo_NoConflict()
    {
        using var context = TestDbHelper.CreateInMemoryContext();
        TestDbHelper.SeedElevator(context);
        var fakeTime = new FakeTimeProvider(BaseUtcNow);
        var now = ToSpainTime(BaseUtcNow);
        TestDbHelper.AddReport(context, 1, ElevatorStatus.Operativo, now.AddMinutes(-5), "ip1");
        var service = new ElevatorStatusService(context, fakeTime);

        var result = await service.GetCurrentStatusAsync(1);

        Assert.Equal(ElevatorStatus.Operativo, result.Status);
        Assert.False(result.HasConflict);
    }

    [Fact]
    public async Task SingleReport_NoOperativo_NoConflict()
    {
        using var context = TestDbHelper.CreateInMemoryContext();
        TestDbHelper.SeedElevator(context);
        var fakeTime = new FakeTimeProvider(BaseUtcNow);
        var now = ToSpainTime(BaseUtcNow);
        TestDbHelper.AddReport(context, 1, ElevatorStatus.NoOperativo, now.AddMinutes(-5), "ip1");
        var service = new ElevatorStatusService(context, fakeTime);

        var result = await service.GetCurrentStatusAsync(1);

        Assert.Equal(ElevatorStatus.NoOperativo, result.Status);
        Assert.False(result.HasConflict);
    }

    [Fact]
    public async Task AllReports_SameStatus_ReturnsThatStatus_NoConflict()
    {
        using var context = TestDbHelper.CreateInMemoryContext();
        TestDbHelper.SeedElevator(context);
        var fakeTime = new FakeTimeProvider(BaseUtcNow);
        var now = ToSpainTime(BaseUtcNow);

        for (int i = 0; i < 5; i++)
            TestDbHelper.AddReport(context, 1, ElevatorStatus.Operativo, now.AddMinutes(-i * 5), $"ip{i}");

        var service = new ElevatorStatusService(context, fakeTime);
        var result = await service.GetCurrentStatusAsync(1);

        Assert.Equal(ElevatorStatus.Operativo, result.Status);
        Assert.False(result.HasConflict);
    }

    [Fact]
    public async Task ClearMajority_Operativo_Wins()
    {
        using var context = TestDbHelper.CreateInMemoryContext();
        TestDbHelper.SeedElevator(context);
        var fakeTime = new FakeTimeProvider(BaseUtcNow);
        var now = ToSpainTime(BaseUtcNow);

        for (int i = 0; i < 4; i++)
            TestDbHelper.AddReport(context, 1, ElevatorStatus.Operativo, now.AddMinutes(-10), $"op{i}");
        TestDbHelper.AddReport(context, 1, ElevatorStatus.NoOperativo, now.AddMinutes(-10), "nop0");

        var service = new ElevatorStatusService(context, fakeTime);
        var result = await service.GetCurrentStatusAsync(1);

        Assert.Equal(ElevatorStatus.Operativo, result.Status);
        Assert.False(result.HasConflict);
    }

    [Fact]
    public async Task ClearMajority_NoOperativo_Wins()
    {
        using var context = TestDbHelper.CreateInMemoryContext();
        TestDbHelper.SeedElevator(context);
        var fakeTime = new FakeTimeProvider(BaseUtcNow);
        var now = ToSpainTime(BaseUtcNow);

        for (int i = 0; i < 4; i++)
            TestDbHelper.AddReport(context, 1, ElevatorStatus.NoOperativo, now.AddMinutes(-10), $"nop{i}");
        TestDbHelper.AddReport(context, 1, ElevatorStatus.Operativo, now.AddMinutes(-10), "op0");

        var service = new ElevatorStatusService(context, fakeTime);
        var result = await service.GetCurrentStatusAsync(1);

        Assert.Equal(ElevatorStatus.NoOperativo, result.Status);
        Assert.False(result.HasConflict);
    }

    [Fact]
    public async Task CloseContest_75Percent_HasConflict()
    {
        using var context = TestDbHelper.CreateInMemoryContext();
        TestDbHelper.SeedElevator(context);
        var fakeTime = new FakeTimeProvider(BaseUtcNow);
        var now = ToSpainTime(BaseUtcNow);

        TestDbHelper.AddReport(context, 1, ElevatorStatus.Operativo, now.AddMinutes(-10), "ipA");
        TestDbHelper.AddReporter(context, "ipA", 4.0);

        TestDbHelper.AddReport(context, 1, ElevatorStatus.NoOperativo, now.AddMinutes(-10), "ipB");
        TestDbHelper.AddReporter(context, "ipB", 3.0);

        var service = new ElevatorStatusService(context, fakeTime);
        var result = await service.GetCurrentStatusAsync(1);

        Assert.Equal(ElevatorStatus.Operativo, result.Status);
        Assert.True(result.HasConflict);
    }

    [Fact]
    public async Task Below75Percent_NoConflict()
    {
        using var context = TestDbHelper.CreateInMemoryContext();
        TestDbHelper.SeedElevator(context);
        var fakeTime = new FakeTimeProvider(BaseUtcNow);
        var now = ToSpainTime(BaseUtcNow);

        TestDbHelper.AddReport(context, 1, ElevatorStatus.Operativo, now.AddMinutes(-10), "ipA");
        TestDbHelper.AddReporter(context, "ipA", 10.0);

        TestDbHelper.AddReport(context, 1, ElevatorStatus.NoOperativo, now.AddMinutes(-10), "ipB");
        TestDbHelper.AddReporter(context, "ipB", 7.4);

        var service = new ElevatorStatusService(context, fakeTime);
        var result = await service.GetCurrentStatusAsync(1);

        Assert.Equal(ElevatorStatus.Operativo, result.Status);
        Assert.False(result.HasConflict);
    }

    [Fact]
    public async Task TimeMultiplier_RecentReportsWeighMore()
    {
        using var context = TestDbHelper.CreateInMemoryContext();
        TestDbHelper.SeedElevator(context);
        var fakeTime = new FakeTimeProvider(BaseUtcNow);
        var now = ToSpainTime(BaseUtcNow);

        TestDbHelper.AddReport(context, 1, ElevatorStatus.NoOperativo, now.AddMinutes(-5), "ip_recent1");
        TestDbHelper.AddReport(context, 1, ElevatorStatus.NoOperativo, now.AddMinutes(-10), "ip_recent2");

        TestDbHelper.AddReport(context, 1, ElevatorStatus.Operativo, now.AddHours(-8), "ip_old1");

        var service = new ElevatorStatusService(context, fakeTime);
        var result = await service.GetCurrentStatusAsync(1);

        Assert.Equal(ElevatorStatus.NoOperativo, result.Status);
    }

    [Fact]
    public async Task OldReports_StillInfluenceResult()
    {
        using var context = TestDbHelper.CreateInMemoryContext();
        TestDbHelper.SeedElevator(context);
        var fakeTime = new FakeTimeProvider(BaseUtcNow);
        var now = ToSpainTime(BaseUtcNow);

        TestDbHelper.AddReport(context, 1, ElevatorStatus.Operativo, now.AddMinutes(-5), "ip1");

        TestDbHelper.AddReport(context, 1, ElevatorStatus.NoOperativo, now.AddHours(-2), "ip2");
        TestDbHelper.AddReport(context, 1, ElevatorStatus.NoOperativo, now.AddHours(-3), "ip3");
        TestDbHelper.AddReport(context, 1, ElevatorStatus.NoOperativo, now.AddHours(-4), "ip4");
        TestDbHelper.AddReport(context, 1, ElevatorStatus.NoOperativo, now.AddHours(-7), "ip5");

        var service = new ElevatorStatusService(context, fakeTime);
        var result = await service.GetCurrentStatusAsync(1);

        Assert.Equal(ElevatorStatus.NoOperativo, result.Status);
    }

    [Fact]
    public async Task VeryOldReports_StillHaveMinimumWeight()
    {
        using var context = TestDbHelper.CreateInMemoryContext();
        TestDbHelper.SeedElevator(context);
        var fakeTime = new FakeTimeProvider(BaseUtcNow);
        var now = ToSpainTime(BaseUtcNow);

        TestDbHelper.AddReport(context, 1, ElevatorStatus.Operativo, now.AddDays(-5), "ip_old");

        var service = new ElevatorStatusService(context, fakeTime);
        var result = await service.GetCurrentStatusAsync(1);

        Assert.Equal(ElevatorStatus.Operativo, result.Status);
        Assert.False(result.HasConflict);
    }

    [Fact]
    public async Task ConflictingReports_ShowsConflict()
    {
        using var context = TestDbHelper.CreateInMemoryContext();
        TestDbHelper.SeedElevator(context);
        var fakeTime = new FakeTimeProvider(BaseUtcNow);
        var now = ToSpainTime(BaseUtcNow);

        TestDbHelper.AddReport(context, 1, ElevatorStatus.Operativo, now.AddMinutes(-5), "ip1");
        TestDbHelper.AddReport(context, 1, ElevatorStatus.NoOperativo, now.AddMinutes(-5), "ip2");

        var service = new ElevatorStatusService(context, fakeTime);
        var result = await service.GetCurrentStatusAsync(1);

        Assert.True(result.HasConflict);
    }

    [Fact]
    public async Task RecentReports_CountsLastHour()
    {
        using var context = TestDbHelper.CreateInMemoryContext();
        TestDbHelper.SeedElevator(context);
        var fakeTime = new FakeTimeProvider(BaseUtcNow);
        var now = ToSpainTime(BaseUtcNow);

        TestDbHelper.AddReport(context, 1, ElevatorStatus.Operativo, now.AddMinutes(-30), "ip1");
        TestDbHelper.AddReport(context, 1, ElevatorStatus.Operativo, now.AddMinutes(-45), "ip2");
        TestDbHelper.AddReport(context, 1, ElevatorStatus.Operativo, now.AddMinutes(-90), "ip3");

        var service = new ElevatorStatusService(context, fakeTime);
        var result = await service.GetCurrentStatusAsync(1);

        Assert.Equal(2, result.RecentReports);
    }

    [Fact]
    public async Task TimeMultiplier_20MinBoundary()
    {
        using var context = TestDbHelper.CreateInMemoryContext();
        TestDbHelper.SeedElevator(context);
        var fakeTime = new FakeTimeProvider(BaseUtcNow);
        var now = ToSpainTime(BaseUtcNow);

        TestDbHelper.AddReport(context, 1, ElevatorStatus.Operativo, now.AddMinutes(-20), "ip_at20_a");
        TestDbHelper.AddReport(context, 1, ElevatorStatus.Operativo, now.AddMinutes(-20), "ip_at20_b");

        TestDbHelper.AddReport(context, 1, ElevatorStatus.NoOperativo, now.AddMinutes(-21), "ip_at21");

        var service = new ElevatorStatusService(context, fakeTime);
        var result = await service.GetCurrentStatusAsync(1);

        Assert.Equal(ElevatorStatus.Operativo, result.Status);
    }

    [Fact]
    public async Task TimeMultiplier_1HourBoundary()
    {
        using var context = TestDbHelper.CreateInMemoryContext();
        TestDbHelper.SeedElevator(context);
        var fakeTime = new FakeTimeProvider(BaseUtcNow);
        var now = ToSpainTime(BaseUtcNow);

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
    public async Task RecentBroken_OutweighsOlderFixed()
    {
        using var context = TestDbHelper.CreateInMemoryContext();
        TestDbHelper.SeedElevator(context);
        var fakeTime = new FakeTimeProvider(BaseUtcNow);
        var now = ToSpainTime(BaseUtcNow);

        TestDbHelper.AddReport(context, 1, ElevatorStatus.Operativo, now.AddDays(-1), "ip_old1");
        TestDbHelper.AddReport(context, 1, ElevatorStatus.Operativo, now.AddDays(-1).AddMinutes(10), "ip_old2");
        TestDbHelper.AddReport(context, 1, ElevatorStatus.Operativo, now.AddDays(-1).AddMinutes(30), "ip_old3");

        TestDbHelper.AddReport(context, 1, ElevatorStatus.NoOperativo, now.AddHours(-4), "ip_broken1");
        TestDbHelper.AddReport(context, 1, ElevatorStatus.NoOperativo, now.AddHours(-2), "ip_broken2");

        var service = new ElevatorStatusService(context, fakeTime);
        var result = await service.GetCurrentStatusAsync(1);

        Assert.Equal(ElevatorStatus.NoOperativo, result.Status);
    }

    [Fact]
    public async Task SingleErroneousReport_DoesNotOverrideMultipleRecent()
    {
        using var context = TestDbHelper.CreateInMemoryContext();
        TestDbHelper.SeedElevator(context);
        var fakeTime = new FakeTimeProvider(BaseUtcNow);
        var now = ToSpainTime(BaseUtcNow);

        TestDbHelper.AddReport(context, 1, ElevatorStatus.Operativo, now.AddMinutes(-5), "ip1");
        TestDbHelper.AddReport(context, 1, ElevatorStatus.Operativo, now.AddMinutes(-10), "ip2");
        TestDbHelper.AddReport(context, 1, ElevatorStatus.Operativo, now.AddHours(-1), "ip3");

        TestDbHelper.AddReport(context, 1, ElevatorStatus.NoOperativo, now, "ip_error");

        var service = new ElevatorStatusService(context, fakeTime);
        var result = await service.GetCurrentStatusAsync(1);

        Assert.Equal(ElevatorStatus.Operativo, result.Status);
    }
}