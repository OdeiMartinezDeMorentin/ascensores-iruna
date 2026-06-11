namespace AscensoresIruna.Api.DTOs;

public class ElevatorDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string CurrentStatus { get; set; } = string.Empty;
    public DateTime? LastReportedAt { get; set; }
    public int TotalReports { get; set; }
    public bool CanReport { get; set; }
}