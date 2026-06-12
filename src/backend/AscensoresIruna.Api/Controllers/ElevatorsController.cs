using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AscensoresIruna.Api.Data;
using AscensoresIruna.Api.DTOs;
using AscensoresIruna.Api.Models;
using AscensoresIruna.Api.Services;

namespace AscensoresIruna.Api.Controllers;

[ApiController]
[Route("api/elevators")]
public class ElevatorsController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IpHashService _ipHashService;
    private readonly ElevatorStatusService _statusService;
    private readonly TrustScoreService _trustService;

    private static readonly TimeSpan RateLimitWindow = TimeSpan.FromMinutes(10);
    private const int MaxElevatorsPerWindow = 3;

    public ElevatorsController(
        AppDbContext context,
        IpHashService ipHashService,
        ElevatorStatusService statusService,
        TrustScoreService trustService)
    {
        _context = context;
        _ipHashService = ipHashService;
        _statusService = statusService;
        _trustService = trustService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ElevatorDto>>> GetElevators()
    {
        var elevators = await _context.Elevators.ToListAsync();
        var ipHash = GetClientIpHash();

        var dtos = new List<ElevatorDto>();
        foreach (var elevator in elevators)
        {
            var dto = await MapToDtoAsync(elevator, ipHash);
            dtos.Add(dto);
        }

        return Ok(dtos);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ElevatorDto>> GetElevator(int id)
    {
        var elevator = await _context.Elevators.FindAsync(id);
        if (elevator is null)
            return NotFound();

        var ipHash = GetClientIpHash();
        return Ok(await MapToDtoAsync(elevator, ipHash));
    }

    [HttpPost("{id}/reports")]
    public async Task<ActionResult<StatusReportDto>> CreateReport(int id, CreateReportDto dto)
    {
        var elevator = await _context.Elevators.FindAsync(id);
        if (elevator is null)
            return NotFound();

        if (!Enum.TryParse<ElevatorStatus>(dto.Status, true, out var status))
            return BadRequest($"Invalid status. Valid values: {string.Join(", ", Enum.GetNames<ElevatorStatus>().Where(s => s != nameof(ElevatorStatus.Desconocido) && s != nameof(ElevatorStatus.Parcial)))}");

        if (status == ElevatorStatus.Desconocido || status == ElevatorStatus.Parcial)
            return BadRequest("Solo puedes reportar los estados 'Operativo' o 'NoOperativo'.");

        var ipHash = GetClientIpHash();
        var now = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("Romance Standard Time"));
        var windowStart = now.Subtract(RateLimitWindow);

        using var transaction = await _context.Database.BeginTransactionAsync(
            System.Data.IsolationLevel.Serializable);

        try
        {
            var recentReportForElevator = await _context.StatusReports
                .AnyAsync(r => r.ElevatorId == id && r.IpAddressHash == ipHash && r.ReportedAt >= windowStart);

            if (recentReportForElevator)
                return StatusCode(429, "Ya has reportado este ascensor en los últimos 10 minutos. Puedes editar tu reporte si lo deseas.");

            var distinctElevatorsInWindow = await _context.StatusReports
                .Where(r => r.IpAddressHash == ipHash && r.ReportedAt >= windowStart)
                .Select(r => r.ElevatorId)
                .Distinct()
                .CountAsync();

            if (distinctElevatorsInWindow >= MaxElevatorsPerWindow)
                return StatusCode(429, "Has alcanzado el máximo de reportes, espera 10 minutos.");

            var report = new StatusReport
            {
                ElevatorId = id,
                Status = status,
                ReportedAt = now,
                IpAddressHash = ipHash
            };

            _context.StatusReports.Add(report);
            await _context.SaveChangesAsync();

            await _trustService.UpdateTrustScoresAsync(id, status, ipHash, now);

            await transaction.CommitAsync();

            return CreatedAtAction(
                nameof(GetElevator),
                new { id },
                new StatusReportDto
                {
                    Id = report.Id,
                    ElevatorId = report.ElevatorId,
                    Status = report.Status.ToString(),
                    ReportedAt = report.ReportedAt
                });
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    [HttpPut("{id}/reports/latest")]
    public async Task<ActionResult<StatusReportDto>> UpdateLatestReport(int id, UpdateReportDto dto)
    {
        var elevator = await _context.Elevators.FindAsync(id);
        if (elevator is null)
            return NotFound();

        if (!Enum.TryParse<ElevatorStatus>(dto.Status, true, out var status))
            return BadRequest($"Invalid status. Valid values: {string.Join(", ", Enum.GetNames<ElevatorStatus>().Where(s => s != nameof(ElevatorStatus.Desconocido) && s != nameof(ElevatorStatus.Parcial)))}");

        if (status == ElevatorStatus.Desconocido || status == ElevatorStatus.Parcial)
            return BadRequest("Solo puedes reportar los estados 'Operativo' o 'NoOperativo'.");

        var ipHash = GetClientIpHash();
        var now = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("Romance Standard Time"));
        var windowStart = now.Subtract(RateLimitWindow);

        var report = await _context.StatusReports
            .Where(r => r.ElevatorId == id && r.IpAddressHash == ipHash && r.ReportedAt >= windowStart)
            .OrderByDescending(r => r.ReportedAt)
            .FirstOrDefaultAsync();

        if (report is null)
            return NotFound("No tienes un reporte reciente para este ascensor.");

        var previousStatus = report.Status;
        report.Status = status;
        await _context.SaveChangesAsync();

        if (previousStatus != status)
            await _trustService.UpdateTrustScoresAsync(id, status, ipHash, report.ReportedAt);

        return Ok(new StatusReportDto
        {
            Id = report.Id,
            ElevatorId = report.ElevatorId,
            Status = report.Status.ToString(),
            ReportedAt = report.ReportedAt
        });
    }

    [HttpDelete("{id}/reports/latest")]
    public async Task<IActionResult> DeleteLatestReport(int id)
    {
        var elevator = await _context.Elevators.FindAsync(id);
        if (elevator is null)
            return NotFound();

        var ipHash = GetClientIpHash();
        var now = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("Romance Standard Time"));
        var windowStart = now.Subtract(RateLimitWindow);

        var report = await _context.StatusReports
            .Where(r => r.ElevatorId == id && r.IpAddressHash == ipHash && r.ReportedAt >= windowStart)
            .OrderByDescending(r => r.ReportedAt)
            .FirstOrDefaultAsync();

        if (report is null)
            return NotFound("No tienes un reporte reciente para este ascensor.");

        _context.StatusReports.Remove(report);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpGet("{id}/reports/my-latest")]
    public async Task<ActionResult<MyLatestReportDto>> GetMyLatestReport(int id)
    {
        var elevator = await _context.Elevators.FindAsync(id);
        if (elevator is null)
            return NotFound();

        var ipHash = GetClientIpHash();
        var now = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("Romance Standard Time"));
        var windowStart = now.Subtract(RateLimitWindow);

        var report = await _context.StatusReports
            .Where(r => r.ElevatorId == id && r.IpAddressHash == ipHash && r.ReportedAt >= windowStart)
            .OrderByDescending(r => r.ReportedAt)
            .FirstOrDefaultAsync();

        if (report is null)
            return NoContent();

        return Ok(new MyLatestReportDto
        {
            ReportId = report.Id,
            Status = report.Status.ToString(),
            ReportedAt = report.ReportedAt,
            CanEdit = true
        });
    }

    private string GetClientIpHash()
    {
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        return _ipHashService.HashIp(ip);
    }

    private async Task<ElevatorDto> MapToDtoAsync(Elevator elevator, string ipHash)
    {
        var statusResult = await _statusService.GetCurrentStatusAsync(elevator.Id);

        var now = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("Romance Standard Time"));
        var windowStart = now.Subtract(RateLimitWindow);

        var canReport = !await _context.StatusReports
            .AnyAsync(r => r.ElevatorId == elevator.Id && r.IpAddressHash == ipHash && r.ReportedAt >= windowStart);

        return new ElevatorDto
        {
            Id = elevator.Id,
            Name = elevator.Name,
            Location = elevator.Location,
            Latitude = elevator.Latitude,
            Longitude = elevator.Longitude,
            CurrentStatus = statusResult.Status.ToString(),
            LastReportedAt = statusResult.LastReportedAt,
            TotalReports = statusResult.TotalReports,
            RecentReports = statusResult.RecentReports,
            CanReport = canReport
        };
    }
}