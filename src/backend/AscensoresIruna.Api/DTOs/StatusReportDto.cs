namespace AscensoresIruna.Api.DTOs;

public class StatusReportDto
{
    public int Id { get; set; }
    public int ElevatorId { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime ReportedAt { get; set; }
}