using AscensoresIruna.Api.Data;
using AscensoresIruna.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace AscensoresIruna.Api.Services;

public class ElevatorStatusService
{
    private readonly AppDbContext _context;
    private readonly TimeProvider _timeProvider;

    private const double ConflictThreshold = 0.75;

    public ElevatorStatusService(AppDbContext context, TimeProvider? timeProvider = null)
    {
        _context = context;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public async Task<ElevatorStatusResult> GetCurrentStatusAsync(int elevatorId)
    {
        var spainTz = TimeZoneInfo.FindSystemTimeZoneById("Romance Standard Time");
        var now = TimeZoneInfo.ConvertTimeFromUtc(_timeProvider.GetUtcNow().UtcDateTime, spainTz);

        var reports = await _context.StatusReports
            .Where(r => r.ElevatorId == elevatorId)
            .OrderByDescending(r => r.ReportedAt)
            .ToListAsync();

        if (reports.Count == 0)
            return new ElevatorStatusResult
            {
                Status = ElevatorStatus.Desconocido,
                HasConflict = false,
                LastReportedAt = null,
                TotalReports = 0,
                RecentReports = 0
            };

        var ipHashes = reports.Select(r => r.IpAddressHash).Distinct().ToList();
        var reporterTrusts = await _context.ReporterIps
            .Where(ri => ipHashes.Contains(ri.IpAddressHash))
            .ToDictionaryAsync(ri => ri.IpAddressHash, ri => ri.TrustScore);

        var mostRecent = reports[0];
        var oneHourAgo = now.AddHours(-1);
        var recentReports = reports.Count(r => r.ReportedAt >= oneHourAgo);

        var operativoWeight = 0.0;
        var noOperativoWeight = 0.0;

        foreach (var report in reports)
        {
            var trust = reporterTrusts.GetValueOrDefault(report.IpAddressHash, 1.0);
            var relativeAge = mostRecent.ReportedAt - report.ReportedAt;
            var timeMultiplier = GetTimeMultiplier(relativeAge);
            var weight = timeMultiplier * trust;

            switch (report.Status)
            {
                case ElevatorStatus.Operativo:
                    operativoWeight += weight;
                    break;
                case ElevatorStatus.NoOperativo:
                    noOperativoWeight += weight;
                    break;
            }
        }

        var totalWeight = operativoWeight + noOperativoWeight;
        if (totalWeight == 0)
            return new ElevatorStatusResult
            {
                Status = ElevatorStatus.Desconocido,
                HasConflict = false,
                LastReportedAt = mostRecent.ReportedAt,
                TotalReports = reports.Count,
                RecentReports = recentReports
            };

        var bestWeight = Math.Max(operativoWeight, noOperativoWeight);
        var bestStatus = operativoWeight >= noOperativoWeight
            ? ElevatorStatus.Operativo
            : ElevatorStatus.NoOperativo;

        var secondWeight = Math.Min(operativoWeight, noOperativoWeight);

        var hasConflict = secondWeight > 0 && secondWeight >= bestWeight * ConflictThreshold;

        return new ElevatorStatusResult
        {
            Status = bestStatus,
            HasConflict = hasConflict,
            LastReportedAt = mostRecent.ReportedAt,
            TotalReports = reports.Count,
            RecentReports = recentReports
        };
    }

    private static double GetTimeMultiplier(TimeSpan relativeAge)
    {
        return relativeAge.TotalHours switch
        {
            <= 0.3333 => 3.0,
            <= 1.0 => 2.0,
            <= 6.0 => 1.0,
            <= 24.0 => 0.5,
            <= 72.0 => 0.25,
            _ => 0.1
        };
    }
}

public class ElevatorStatusResult
{
    public ElevatorStatus Status { get; set; }
    public bool HasConflict { get; set; }
    public DateTime? LastReportedAt { get; set; }
    public int TotalReports { get; set; }
    public int RecentReports { get; set; }
}