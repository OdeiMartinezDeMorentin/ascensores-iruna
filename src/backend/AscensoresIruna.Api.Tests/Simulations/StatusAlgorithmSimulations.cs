using AscensoresIruna.Api.Models;
using AscensoresIruna.Api.Services;
using AscensoresIruna.Api.Tests.Helpers;

namespace AscensoresIruna.Api.Tests.Simulations;

public class StatusAlgorithmSimulations
{
    private static DateTimeOffset BaseUtcNow => new(2026, 6, 12, 12, 0, 0, TimeSpan.Zero);

    private static DateTime ToSpainTime(DateTimeOffset utc)
    {
        var spainTz = TimeZoneInfo.FindSystemTimeZoneById("Romance Standard Time");
        return TimeZoneInfo.ConvertTimeFromUtc(utc.UtcDateTime, spainTz);
    }

    [Fact]
    public async Task ManyTrolls_ReportingOppositeStatus()
    {
        using var context = TestDbHelper.CreateInMemoryContext();
        TestDbHelper.SeedElevator(context);
        var fakeTime = new FakeTimeProvider(BaseUtcNow);
        var now = ToSpainTime(BaseUtcNow);

        for (int i = 0; i < 5; i++)
        {
            var ip = $"troll_{i}";
            TestDbHelper.AddReport(context, 1, ElevatorStatus.NoOperativo, now.AddMinutes(-5), ip);
            TestDbHelper.AddReporter(context, ip, 0.1, contradictions: 10);
        }

        for (int i = 0; i < 2; i++)
        {
            var ip = $"real_{i}";
            TestDbHelper.AddReport(context, 1, ElevatorStatus.Operativo, now.AddMinutes(-5), ip);
            TestDbHelper.AddReporter(context, ip, 3.0, confirmations: 10);
        }

        var service = new ElevatorStatusService(context, fakeTime);
        var result = await service.GetCurrentStatusAsync(1);

        Assert.Equal(ElevatorStatus.Operativo, result.Status);
    }

    [Fact]
    public async Task ManyRealUsers_ReportingCorrectly()
    {
        using var context = TestDbHelper.CreateInMemoryContext();
        TestDbHelper.SeedElevator(context);
        var fakeTime = new FakeTimeProvider(BaseUtcNow);
        var now = ToSpainTime(BaseUtcNow);

        var trustService = new TrustScoreService(context);

        for (int i = 0; i < 10; i++)
        {
            var ip = $"user_{i}";
            var reportTime = now.AddMinutes(-i * 2);
            TestDbHelper.AddReport(context, 1, ElevatorStatus.Operativo, reportTime, ip);
            await trustService.UpdateTrustScoresAsync(1, ElevatorStatus.Operativo, ip, reportTime);
        }

        var service = new ElevatorStatusService(context, fakeTime);
        var result = await service.GetCurrentStatusAsync(1);

        Assert.Equal(ElevatorStatus.Operativo, result.Status);

        var firstUser = context.ReporterIps.Find("user_0");
        Assert.NotNull(firstUser);
        Assert.True(firstUser.TrustScore >= 1.0);
    }

    [Fact]
    public async Task ElevatorBreaksDown_StatusChanges()
    {
        using var context = TestDbHelper.CreateInMemoryContext();
        TestDbHelper.SeedElevator(context);
        var fakeTime = new FakeTimeProvider(BaseUtcNow);
        var now = ToSpainTime(BaseUtcNow);

        for (int i = 0; i < 3; i++)
            TestDbHelper.AddReport(context, 1, ElevatorStatus.Operativo, now.AddMinutes(-50), $"early_op_{i}");

        var service = new ElevatorStatusService(context, fakeTime);
        var result1 = await service.GetCurrentStatusAsync(1);
        Assert.Equal(ElevatorStatus.Operativo, result1.Status);

        for (int i = 0; i < 4; i++)
            TestDbHelper.AddReport(context, 1, ElevatorStatus.NoOperativo, now.AddMinutes(-5), $"break_nop_{i}");

        var result2 = await service.GetCurrentStatusAsync(1);
        Assert.Equal(ElevatorStatus.NoOperativo, result2.Status);
    }

    [Fact]
    public async Task FewUsers_SingleReport_Accepted()
    {
        using var context = TestDbHelper.CreateInMemoryContext();
        TestDbHelper.SeedElevator(context);
        var fakeTime = new FakeTimeProvider(BaseUtcNow);
        var now = ToSpainTime(BaseUtcNow);

        TestDbHelper.AddReport(context, 1, ElevatorStatus.NoOperativo, now.AddMinutes(-15), "solo_user");

        var service = new ElevatorStatusService(context, fakeTime);
        var result = await service.GetCurrentStatusAsync(1);

        Assert.Equal(ElevatorStatus.NoOperativo, result.Status);
    }

    [Fact]
    public async Task FewUsers_TwoReports_SameStatus()
    {
        using var context = TestDbHelper.CreateInMemoryContext();
        TestDbHelper.SeedElevator(context);
        var fakeTime = new FakeTimeProvider(BaseUtcNow);
        var now = ToSpainTime(BaseUtcNow);

        TestDbHelper.AddReport(context, 1, ElevatorStatus.Operativo, now.AddMinutes(-10), "user_a");
        TestDbHelper.AddReport(context, 1, ElevatorStatus.Operativo, now.AddMinutes(-15), "user_b");

        var service = new ElevatorStatusService(context, fakeTime);
        var result = await service.GetCurrentStatusAsync(1);

        Assert.Equal(ElevatorStatus.Operativo, result.Status);
    }

    [Fact]
    public async Task ManyUsers_ContentiousReports_HasConflict()
    {
        using var context = TestDbHelper.CreateInMemoryContext();
        TestDbHelper.SeedElevator(context);
        var fakeTime = new FakeTimeProvider(BaseUtcNow);
        var now = ToSpainTime(BaseUtcNow);

        for (int i = 0; i < 5; i++)
            TestDbHelper.AddReport(context, 1, ElevatorStatus.Operativo, now.AddMinutes(-10), $"op_{i}");

        for (int i = 0; i < 5; i++)
            TestDbHelper.AddReport(context, 1, ElevatorStatus.NoOperativo, now.AddMinutes(-10), $"nop_{i}");

        var service = new ElevatorStatusService(context, fakeTime);
        var result = await service.GetCurrentStatusAsync(1);

        Assert.True(result.HasConflict);
    }

    [Fact]
    public async Task ThresholdEdge_Exactly75Percent()
    {
        using var context = TestDbHelper.CreateInMemoryContext();
        TestDbHelper.SeedElevator(context);
        var fakeTime = new FakeTimeProvider(BaseUtcNow);
        var now = ToSpainTime(BaseUtcNow);

        TestDbHelper.AddReport(context, 1, ElevatorStatus.NoOperativo, now.AddMinutes(-10), "ipWinner");
        TestDbHelper.AddReporter(context, "ipWinner", 1.0);

        TestDbHelper.AddReport(context, 1, ElevatorStatus.Operativo, now.AddMinutes(-10), "ipLoser");
        TestDbHelper.AddReporter(context, "ipLoser", 0.75);

        var service = new ElevatorStatusService(context, fakeTime);
        var result = await service.GetCurrentStatusAsync(1);

        Assert.Equal(ElevatorStatus.NoOperativo, result.Status);
        Assert.True(result.HasConflict);
    }

    [Fact]
    public async Task ThresholdEdge_JustBelow75Percent()
    {
        using var context = TestDbHelper.CreateInMemoryContext();
        TestDbHelper.SeedElevator(context);
        var fakeTime = new FakeTimeProvider(BaseUtcNow);
        var now = ToSpainTime(BaseUtcNow);

        TestDbHelper.AddReport(context, 1, ElevatorStatus.NoOperativo, now.AddMinutes(-10), "ipWinner");
        TestDbHelper.AddReporter(context, "ipWinner", 1.0);

        TestDbHelper.AddReport(context, 1, ElevatorStatus.Operativo, now.AddMinutes(-10), "ipLoser");
        TestDbHelper.AddReporter(context, "ipLoser", 0.74);

        var service = new ElevatorStatusService(context, fakeTime);
        var result = await service.GetCurrentStatusAsync(1);

        Assert.Equal(ElevatorStatus.NoOperativo, result.Status);
        Assert.False(result.HasConflict);
    }

    [Fact]
    public async Task TrustDecay_TrollsLoseTrustOverTime()
    {
        using var context = TestDbHelper.CreateInMemoryContext();
        TestDbHelper.SeedElevator(context);
        var fakeTime = new FakeTimeProvider(BaseUtcNow);
        var now = ToSpainTime(BaseUtcNow);

        var trustService = new TrustScoreService(context);

        TestDbHelper.AddReport(context, 1, ElevatorStatus.Operativo, now.AddMinutes(-25), "real_user_1");

        TestDbHelper.AddReport(context, 1, ElevatorStatus.NoOperativo, now.AddMinutes(-20), "troll");
        await trustService.UpdateTrustScoresAsync(1, ElevatorStatus.NoOperativo, "troll", now.AddMinutes(-20));

        var trollAfter1 = context.ReporterIps.Find("troll")!;
        Assert.Equal(1, trollAfter1.Contradictions);

        TestDbHelper.AddReport(context, 1, ElevatorStatus.Operativo, now.AddMinutes(-15), "real_user_2");
        await trustService.UpdateTrustScoresAsync(1, ElevatorStatus.Operativo, "real_user_2", now.AddMinutes(-15));

        context.ChangeTracker.Clear();

        var trollFinal = context.ReporterIps.Find("troll")!;
        Assert.True(trollFinal.TrustScore < 1.0);
        Assert.True(trollFinal.Contradictions >= 1);
    }

    [Fact]
    public async Task TimeDecay_OldReportsLoseInfluence()
    {
        using var context = TestDbHelper.CreateInMemoryContext();
        TestDbHelper.SeedElevator(context);
        var now = ToSpainTime(BaseUtcNow);

        for (int i = 0; i < 3; i++)
            TestDbHelper.AddReport(context, 1, ElevatorStatus.NoOperativo, now.AddMinutes(-100), $"old_nop_{i}");

        for (int i = 0; i < 3; i++)
            TestDbHelper.AddReport(context, 1, ElevatorStatus.Operativo, now.AddMinutes(-5), $"new_op_{i}");

        var fakeTime = new FakeTimeProvider(BaseUtcNow);
        var service = new ElevatorStatusService(context, fakeTime);
        var result = await service.GetCurrentStatusAsync(1);

        Assert.Equal(ElevatorStatus.Operativo, result.Status);
    }
}
