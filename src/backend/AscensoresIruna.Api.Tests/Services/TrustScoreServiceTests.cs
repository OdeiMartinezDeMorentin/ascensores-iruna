using AscensoresIruna.Api.Models;
using AscensoresIruna.Api.Services;
using AscensoresIruna.Api.Tests.Helpers;

namespace AscensoresIruna.Api.Tests.Services;

public class TrustScoreServiceTests
{
    [Fact]
    public void CalculateTrust_Default_Returns1()
    {
        var result = TrustScoreService.CalculateTrust(0, 0);
        Assert.Equal(1.0, result);
    }

    [Fact]
    public void CalculateTrust_Confirmations_Increase()
    {
        var result = TrustScoreService.CalculateTrust(5, 0);
        Assert.Equal(2.0, result);
    }

    [Fact]
    public void CalculateTrust_Contradictions_Decrease()
    {
        var result = TrustScoreService.CalculateTrust(0, 3);
        Assert.Equal(0.1, result, precision: 5);
    }

    [Fact]
    public void CalculateTrust_Clamped_Max3()
    {
        var result = TrustScoreService.CalculateTrust(20, 0);
        Assert.Equal(3.0, result);
    }

    [Fact]
    public void CalculateTrust_Clamped_Min01()
    {
        var result = TrustScoreService.CalculateTrust(0, 10);
        Assert.Equal(0.1, result);
    }

    [Fact]
    public void CalculateTrust_Mixed()
    {
        var result = TrustScoreService.CalculateTrust(3, 2);
        Assert.Equal(1.0, result);
    }

    [Fact]
    public async Task UpdateTrust_Confirmation_BothIpsIncrease()
    {
        using var context = TestDbHelper.CreateInMemoryContext();
        TestDbHelper.SeedElevator(context);
        var now = DateTime.UtcNow;

        TestDbHelper.AddReport(context, 1, ElevatorStatus.Operativo, now.AddMinutes(-10), "ipA");

        var service = new TrustScoreService(context);
        await service.UpdateTrustScoresAsync(1, ElevatorStatus.Operativo, "ipB", now);

        var reporterA = context.ReporterIps.Find("ipA");
        var reporterB = context.ReporterIps.Find("ipB");

        Assert.NotNull(reporterA);
        Assert.NotNull(reporterB);
        Assert.Equal(1, reporterA.Confirmations);
        Assert.Equal(1, reporterB.Confirmations);
        Assert.Equal(0, reporterA.Contradictions);
        Assert.Equal(0, reporterB.Contradictions);
        Assert.True(reporterA.TrustScore > 1.0);
        Assert.True(reporterB.TrustScore > 1.0);
    }

    [Fact]
    public async Task UpdateTrust_Contradiction_BothIpsGetContradiction()
    {
        using var context = TestDbHelper.CreateInMemoryContext();
        TestDbHelper.SeedElevator(context);
        var now = DateTime.UtcNow;

        TestDbHelper.AddReport(context, 1, ElevatorStatus.Operativo, now.AddMinutes(-10), "ipA");

        var service = new TrustScoreService(context);
        await service.UpdateTrustScoresAsync(1, ElevatorStatus.NoOperativo, "ipB", now);

        var reporterA = context.ReporterIps.Find("ipA");
        var reporterB = context.ReporterIps.Find("ipB");

        Assert.NotNull(reporterA);
        Assert.NotNull(reporterB);
        Assert.Equal(1, reporterA.Contradictions);
        Assert.Equal(1, reporterB.Contradictions);
        Assert.True(reporterA.TrustScore < 1.0);
        Assert.True(reporterB.TrustScore < 1.0);
    }

    [Fact]
    public async Task UpdateTrust_NoPreviousReports_NewIpCreated()
    {
        using var context = TestDbHelper.CreateInMemoryContext();
        TestDbHelper.SeedElevator(context);
        var now = DateTime.UtcNow;

        var service = new TrustScoreService(context);
        await service.UpdateTrustScoresAsync(1, ElevatorStatus.Operativo, "ipNew", now);

        var reporter = context.ReporterIps.Find("ipNew");
        Assert.NotNull(reporter);
        Assert.Equal(1.0, reporter.TrustScore);
        Assert.Equal(0, reporter.Confirmations);
        Assert.Equal(0, reporter.Contradictions);
    }

    [Fact]
    public async Task UpdateTrust_OutsideWindow_NotAffected()
    {
        using var context = TestDbHelper.CreateInMemoryContext();
        TestDbHelper.SeedElevator(context);
        var now = DateTime.UtcNow;

        TestDbHelper.AddReport(context, 1, ElevatorStatus.Operativo, now.AddMinutes(-35), "ipOld");
        TestDbHelper.AddReporter(context, "ipOld", 1.0);

        var service = new TrustScoreService(context);
        await service.UpdateTrustScoresAsync(1, ElevatorStatus.NoOperativo, "ipNew", now);

        var reporterOld = context.ReporterIps.Find("ipOld");
        Assert.NotNull(reporterOld);
        Assert.Equal(1.0, reporterOld.TrustScore);
        Assert.Equal(0, reporterOld.Confirmations);
        Assert.Equal(0, reporterOld.Contradictions);
    }
}
