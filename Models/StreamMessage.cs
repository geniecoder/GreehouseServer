namespace GreenhouseGuard.Server.Models;

public class StreamMessage
{
    public string Type { get; set; } = "reading_delta";
    public long Seq { get; set; }
    public int Version { get; set; }
    public DateTime Timestamp { get; set; }
    public string GreenhouseId { get; set; } = "north-glasshouse-block-a";
    public SensorReading? Reading { get; set; }
    public AnomalyEvent? Event { get; set; }
    public string? Status { get; set; }
    public string? Summary { get; set; }
}