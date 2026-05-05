namespace GreenhouseGuard.Server.Models;

public class SensorReading
{
    public double Temperature { get; set; }
    public double Humidity { get; set; }
    public int Co2 { get; set; }
}