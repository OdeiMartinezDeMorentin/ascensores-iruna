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

        var operativoWeight = 0.0;
        var noOperativoWeight = 0.0;
        var parcialWeight = 0.0;

        foreach (var report in reports)
        {
            var trust = reporterTrusts.GetValueOrDefault(report.IpAddressHash, 1.0);
            var timeMultiplier = GetTimeMultiplier(now - report.ReportedAt);
            var weight = timeMultiplier * trust;

            switch (report.Status)
            {
                case ElevatorStatus.Operativo:
                    operativoWeight += weight;
                    break;
                case ElevatorStatus.NoOperativo:
                    noOperativoWeight += weight;
                    break;
                case ElevatorStatus.Parcial:
                    parcialWeight += weight;
                    break;
            }
        }

        if (onlyHasOneReportEver && reports.Count == 1)
        {
            var firstReport = reports.Last();
            return new ElevatorStatusResult
            {
                Status = firstReport.Status,
                LastReportedAt = firstReport.ReportedAt,
                TotalReports = await _context.StatusReports.CountAsync(r => r.ElevatorId == elevatorId)
            };
        }

        var totalWeight = operativoWeight + noOperativoWeight + parcialWeight;
        if (totalWeight == 0)
            return new ElevatorStatusResult
            {
                Status = ElevatorStatus.Desconocido,
                LastReportedAt = reports.First().ReportedAt,
                TotalReports = await _context.StatusReports.CountAsync(r => r.ElevatorId == elevatorId)
            };

        var bestWeight = Math.Max(operativoWeight, noOperativoWeight);
        if (bestWeight == 0)
        {
            return new ElevatorStatusResult
            {
                Status = parcialWeight > 0 ? ElevatorStatus.Parcial : ElevatorStatus.Desconocido,
                LastReportedAt = reports.First().ReportedAt,
                TotalReports = await _context.StatusReports.CountAsync(r => r.ElevatorId == elevatorId)
            };
        }

        var secondWeight = Math.Min(operativoWeight, noOperativoWeight);
        var bestStatus = operativoWeight >= noOperativoWeight
            ? ElevatorStatus.Operativo
            : ElevatorStatus.NoOperativo;

        if (secondWeight > 0 && secondWeight >= bestWeight * 0.6)
        {
            bestStatus = ElevatorStatus.Parcial;
        }

        return new ElevatorStatusResult
        {
            Status = bestStatus,
            LastReportedAt = reports.First().ReportedAt,
            TotalReports = await _context.StatusReports.CountAsync(r => r.ElevatorId == elevatorId)
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