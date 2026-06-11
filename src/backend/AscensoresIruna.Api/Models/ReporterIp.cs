namespace AscensoresIruna.Api.Models;

public class ReporterIp
{
    public string IpAddressHash { get; set; } = string.Empty;
    public double TrustScore { get; set; } = 1.0;
    public int Confirmations { get; set; }
    public int Contradictions { get; set; }
    public DateTime LastSeenAt { get; set; }
}