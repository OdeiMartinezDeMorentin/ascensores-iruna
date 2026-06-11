using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AscensoresIruna.Api.Data;
using AscensoresIruna.Api.DTOs;
using AscensoresIruna.Api.Models;

namespace AscensoresIruna.Api.Controllers;

[ApiController]
[Route("api/elevators")]
public class ElevatorsController : ControllerBase
{
    private readonly AppDbContext _context;

    public ElevatorsController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ElevatorDto>>> GetElevators()
    {
        var elevators = await _context.Elevators
            .Include(e => e.StatusReports)
            .ToListAsync();

        var dtos = elevators.Select(MapToDto).ToList();
        return Ok(dtos);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ElevatorDto>> GetElevator(int id)
    {
        var elevator = await _context.Elevators
            .Include(e => e.StatusReports)
            .FirstOrDefaultAsync(e => e.Id == id);

        if (elevator is null)
            return NotFound();

        return Ok(MapToDto(elevator));
    }

    [HttpPost("{id}/reports")]
    public async Task<ActionResult<StatusReportDto>> CreateReport(int id, CreateReportDto dto)
    {
        var elevator = await _context.Elevators.FindAsync(id);
        if (elevator is null)
            return NotFound();

        if (!Enum.TryParse<ElevatorStatus>(dto.Status, true, out var status))
            return BadRequest($"Invalid status. Valid values: {string.Join(", ", Enum.GetNames<ElevatorStatus>().Where(s => s != nameof(ElevatorStatus.Desconocido)))}");

        if (status == ElevatorStatus.Desconocido)
            return BadRequest("Cannot report 'Desconocido' status.");

        var report = new StatusReport
        {
            ElevatorId = id,
            Status = status,
            ReportedAt = DateTime.UtcNow
        };

        _context.StatusReports.Add(report);
        await _context.SaveChangesAsync();

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

    private static ElevatorDto MapToDto(Elevator elevator)
    {
        var latestReport = elevator.StatusReports
            .OrderByDescending(r => r.ReportedAt)
            .FirstOrDefault();

        var currentStatus = latestReport is not null
            ? latestReport.Status.ToString()
            : ElevatorStatus.Desconocido.ToString();

        return new ElevatorDto
        {
            Id = elevator.Id,
            Name = elevator.Name,
            Location = elevator.Location,
            Latitude = elevator.Latitude,
            Longitude = elevator.Longitude,
            CurrentStatus = currentStatus,
            LastReportedAt = latestReport?.ReportedAt,
            TotalReports = elevator.StatusReports.Count
        };
    }
}