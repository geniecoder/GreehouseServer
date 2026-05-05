namespace GreenhouseGuard.Server.Models;

public class SnapshotResponse
{
    public string Type { get; set; } = "snapshot";
    public long Seq { get; set; }
    public int Version { get; set; }
    public DateTime Timestamp { get; set; }
    public string GreenhouseId { get; set; } = "north-glasshouse-block-a";
    public string Status { get; set; } = "LIVE";
    public string Summary { get; set; } = "All sensors operating normally";
    public SensorReading Current { get; set; } = new();
    public List<SensorHistoryItem> History { get; set; } = new();
    public List<AnomalyEvent> Anomalies { get; set; } = new();
}

public class SensorHistoryItem
{
    public long Seq { get; set; }
    public DateTime Timestamp { get; set; }
    public SensorReading Reading { get; set; } = new();
}