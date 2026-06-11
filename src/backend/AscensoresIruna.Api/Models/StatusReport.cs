namespace AscensoresIruna.Api.Models;

public class StatusReport
{
    public int Id { get; set; }
    public int ElevatorId { get; set; }
    public ElevatorStatus Status { get; set; }
    public DateTime ReportedAt { get; set; }
    public Elevator Elevator { get; set; } = null!;
}