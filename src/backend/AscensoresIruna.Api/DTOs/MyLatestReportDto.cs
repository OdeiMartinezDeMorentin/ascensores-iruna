namespace AscensoresIruna.Api.DTOs;

public class MyLatestReportDto
{
    public int ReportId { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime ReportedAt { get; set; }
    public bool CanEdit { get; set; }
}