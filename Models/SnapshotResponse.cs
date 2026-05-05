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
    public SensorRanges Ranges { get; set; } = new();
}

public class SensorRanges
{
    public SensorRange Temperature { get; set; } = new();
    public SensorRange Humidity { get; set; } = new();
    public SensorRange Co2 { get; set; } = new();
}

public class SensorRange
{
    public double Min { get; set; }
    public double Max { get; set; }
}

public class SensorHistoryItem
{
    public long Seq { get; set; }
    public DateTime Timestamp { get; set; }
    public SensorReading Reading { get; set; } = new();
}