using AscensoresIruna.Api.Data;
using AscensoresIruna.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace AscensoresIruna.Api.Services;

public class ElevatorStatusService
{
    private readonly AppDbContext _context;

    public ElevatorStatusService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<ElevatorStatusResult> GetCurrentStatusAsync(int elevatorId)
    {
        var spainTz = TimeZoneInfo.FindSystemTimeZoneById("Romance Standard Time");
        var now = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, spainTz);
        var twoHoursAgo = now.AddHours(-2);

        var reports = await _context.StatusReports
            .Where(r => r.ElevatorId == elevatorId && r.ReportedAt >= twoHoursAgo)
            .OrderByDescending(r => r.ReportedAt)
            .ToListAsync();

        if (reports.Count == 0)
            return new ElevatorStatusResult
            {
                Status = ElevatorStatus.Desconocido,
                LastReportedAt = null,
                TotalReports = await _context.StatusReports.CountAsync(r => r.ElevatorId == elevatorId)
            };

        var ipHashes = reports.Select(r => r.IpAddressHash).Distinct().ToList();
        var reporterTrusts = await _context.ReporterIps
            .Where(ri => ipHashes.Contains(ri.IpAddressHash))
            .ToDictionaryAsync(ri => ri.IpAddressHash, ri => ri.TrustScore);

        bool onlyHasOneReportEver = !await _context.StatusReports
            .AnyAsync(r => r.ElevatorId == elevatorId && r.ReportedAt < twoHoursAgo);

        var statusWeights = new Dictionary<ElevatorStatus, double>
        {
            [ElevatorStatus.Operativo] = 0,
            [ElevatorStatus.Parcial] = 0,
            [ElevatorStatus.Averiado] = 0
        };

        foreach (var report in reports)
        {
            var trust = reporterTrusts.GetValueOrDefault(report.IpAddressHash, 1.0);
            var timeMultiplier = GetTimeMultiplier(now - report.ReportedAt);
            statusWeights[report.Status] += timeMultiplier * trust;
        }

        var firstReport = reports.Last();
        var totalReportsBefore = await _context.StatusReports
            .CountAsync(r => r.ElevatorId == elevatorId);

        if (onlyHasOneReportEver && reports.Count == 1)
        {
            return new ElevatorStatusResult
            {
                Status = firstReport.Status,
                LastReportedAt = firstReport.ReportedAt,
                TotalReports = totalReportsBefore
            };
        }

        var bestStatus = statusWeights
            .Where(kvp => kvp.Value > 0)
            .OrderByDescending(kvp => kvp.Value)
            .FirstOrDefault();

        var currentStatus = bestStatus.Value > 0
            ? bestStatus.Key
            : ElevatorStatus.Desconocido;

        return new ElevatorStatusResult
        {
            Status = currentStatus,
            LastReportedAt = reports.First().ReportedAt,
            TotalReports = totalReportsBefore
        };
    }

    private static double GetTimeMultiplier(TimeSpan age)
    {
        return age.TotalMinutes switch
        {
            <= 20 => 3.0,
            <= 60 => 2.0,
            _ => 1.0
        };
    }
}

public class ElevatorStatusResult
{
    public ElevatorStatus Status { get; set; }
    public DateTime? LastReportedAt { get; set; }
    public int TotalReports { get; set; }
}