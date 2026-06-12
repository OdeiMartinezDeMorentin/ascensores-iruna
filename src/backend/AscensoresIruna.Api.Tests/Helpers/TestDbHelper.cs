using AscensoresIruna.Api.Data;
using AscensoresIruna.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace AscensoresIruna.Api.Tests.Helpers;

public static class TestDbHelper
{
    public static AppDbContext CreateInMemoryContext(string? dbName = null)
    {
        dbName ??= Guid.NewGuid().ToString();
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new AppDbContext(options);
    }

    public static Elevator SeedElevator(AppDbContext context, int id = 1)
    {
        var elevator = new Elevator
        {
            Id = id,
            Name = $"Test Elevator {id}",
            Location = $"Test Location {id}",
            Latitude = 42.8125,
            Longitude = -1.6458
        };
        context.Elevators.Add(elevator);
        context.SaveChanges();
        return elevator;
    }

    public static StatusReport AddReport(
        AppDbContext context,
        int elevatorId,
        ElevatorStatus status,
        DateTime reportedAt,
        string ipHash)
    {
        var report = new StatusReport
        {
            ElevatorId = elevatorId,
            Status = status,
            ReportedAt = reportedAt,
            IpAddressHash = ipHash
        };
        context.StatusReports.Add(report);
        context.SaveChanges();
        return report;
    }

    public static ReporterIp AddReporter(
        AppDbContext context,
        string ipHash,
        double trustScore = 1.0,
        int confirmations = 0,
        int contradictions = 0)
    {
        var reporter = new ReporterIp
        {
            IpAddressHash = ipHash,
            TrustScore = trustScore,
            Confirmations = confirmations,
            Contradictions = contradictions,
            LastSeenAt = DateTime.UtcNow
        };
        context.ReporterIps.Add(reporter);
        context.SaveChanges();
        return reporter;
    }
}

public class FakeTimeProvider : TimeProvider
{
    private DateTimeOffset _utcNow;

    public FakeTimeProvider(DateTimeOffset utcNow)
    {
        _utcNow = utcNow;
    }

    public override DateTimeOffset GetUtcNow() => _utcNow;

    public void Advance(TimeSpan duration) => _utcNow = _utcNow.Add(duration);

    public void SetUtcNow(DateTimeOffset utcNow) => _utcNow = utcNow;
}
