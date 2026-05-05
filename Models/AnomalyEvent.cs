namespace GreenhouseGuard.Server.Models;

public class AnomalyEvent
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public long Seq { get; set; }
    public DateTime Timestamp { get; set; }
    public string Sensor { get; set; } = "";
    public string Level { get; set; } = "info";
    public double Value { get; set; }
    public string Reason { get; set; } = "";
    public string Message { get; set; } = "";
}