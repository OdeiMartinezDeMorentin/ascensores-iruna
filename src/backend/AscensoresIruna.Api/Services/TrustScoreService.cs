using AscensoresIruna.Api.Data;
using AscensoresIruna.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace AscensoresIruna.Api.Services;

public class TrustScoreService
{
    private readonly AppDbContext _context;

    public TrustScoreService(AppDbContext context)
    {
        _context = context;
    }

    public async Task UpdateTrustScoresAsync(int elevatorId, ElevatorStatus newStatus, string newIpHash, DateTime reportedAt)
    {
        var thirtyMinutesBefore = reportedAt.AddMinutes(-30);

        var previousReports = await _context.StatusReports
            .Where(r => r.ElevatorId == elevatorId
                     && r.ReportedAt >= thirtyMinutesBefore
                     && r.ReportedAt < reportedAt
                     && r.IpAddressHash != newIpHash)
            .ToListAsync();

        var allIpHashes = previousReports.Select(r => r.IpAddressHash).ToList();
        allIpHashes.Add(newIpHash);
        allIpHashes = allIpHashes.Distinct().ToList();

        var reporters = await _context.ReporterIps
            .Where(ri => allIpHashes.Contains(ri.IpAddressHash))
            .ToDictionaryAsync(ri => ri.IpAddressHash);

        foreach (var prevReport in previousReports)
        {
            bool isConfirmation = prevReport.Status == newStatus;

            if (!reporters.TryGetValue(prevReport.IpAddressHash, out var prevReporter))
            {
                prevReporter = new ReporterIp
                {
                    IpAddressHash = prevReport.IpAddressHash,
                    LastSeenAt = prevReport.ReportedAt
                };
                _context.ReporterIps.Add(prevReporter);
                reporters[prevReporter.IpAddressHash] = prevReporter;
            }

            if (isConfirmation)
            {
                prevReporter.Confirmations++;
                prevReporter.TrustScore = CalculateTrust(prevReporter.Confirmations, prevReporter.Contradictions);

                if (!reporters.TryGetValue(newIpHash, out var newReporter))
                {
                    newReporter = new ReporterIp
                    {
                        IpAddressHash = newIpHash,
                        LastSeenAt = reportedAt
                    };
                    _context.ReporterIps.Add(newReporter);
                    reporters[newIpHash] = newReporter;
                }
                newReporter.Confirmations++;
                newReporter.TrustScore = CalculateTrust(newReporter.Confirmations, newReporter.Contradictions);
            }
            else
            {
                prevReporter.Contradictions++;
                prevReporter.TrustScore = CalculateTrust(prevReporter.Confirmations, prevReporter.Contradictions);

                if (!reporters.TryGetValue(newIpHash, out var newReporter))
                {
                    newReporter = new ReporterIp
                    {
                        IpAddressHash = newIpHash,
                        LastSeenAt = reportedAt
                    };
                    _context.ReporterIps.Add(newReporter);
                    reporters[newIpHash] = newReporter;
                }
                newReporter.Contradictions++;
                newReporter.TrustScore = CalculateTrust(newReporter.Confirmations, newReporter.Contradictions);
            }
        }

        if (!reporters.ContainsKey(newIpHash))
        {
            var newReporter = new ReporterIp
            {
                IpAddressHash = newIpHash,
                LastSeenAt = reportedAt,
                TrustScore = 1.0
            };
            _context.ReporterIps.Add(newReporter);
        }
        else
        {
            var existing = reporters[newIpHash];
            existing.LastSeenAt = reportedAt;
        }

        await _context.SaveChangesAsync();
    }

    public static double CalculateTrust(int confirmations, int contradictions)
    {
        return Math.Clamp(1.0 + 0.2 * confirmations - 0.3 * contradictions, 0.1, 3.0);
    }
}