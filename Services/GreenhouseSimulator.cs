using GreenhouseGuard.Server.Models;

namespace GreenhouseGuard.Server.Services;

public class GreenhouseSimulator
{
    private readonly Random _random = new();

    private const int Version = 3;
    private const string GreenhouseId = "north-glasshouse-block-a";

    private long _seq = 44800;

    private SensorReading _current = new()
    {
        Temperature = 23.4,
        Humidity = 64,
        Co2 = 760
    };

    private readonly List<SensorHistoryItem> _readingHistory = new();
    private readonly List<AnomalyEvent> _anomalyHistory = new();

    public GreenhouseSimulator()
    {
        GenerateInitialHistory();
    }

    public SnapshotResponse GetSnapshot()
    {
        return new SnapshotResponse
        {
            Type = "snapshot",
            Seq = _seq,
            Version = Version,
            Timestamp = DateTime.UtcNow,
            GreenhouseId = GreenhouseId,
            Status = GetStatus(),
            Summary = GetSummary(_current),
            Current = _current,
            History = _readingHistory.TakeLast(100).ToList(),
            Anomalies = _anomalyHistory.TakeLast(50).ToList()
        };
    }

    public List<StreamMessage> GetNextMessages()
    {
        var messages = new List<StreamMessage>();

        _seq++;

        _current = CreateNextReading();

        var historyItem = new SensorHistoryItem
        {
            Seq = _seq,
            Timestamp = DateTime.UtcNow,
            Reading = _current
        };

        _readingHistory.Add(historyItem);

        if (_readingHistory.Count > 500)
        {
            _readingHistory.RemoveAt(0);
        }

        messages.Add(new StreamMessage
        {
            Type = "reading_delta",
            Seq = _seq,
            Version = Version,
            Timestamp = DateTime.UtcNow,
            GreenhouseId = GreenhouseId,
            Reading = _current,
            Status = GetStatus(),
            Summary = GetSummary(_current)
        });

        var anomaly = DetectAnomaly(_current);

        if (anomaly != null)
        {
            anomaly.Seq = _seq;
            anomaly.Timestamp = DateTime.UtcNow;

            _anomalyHistory.Add(anomaly);

            if (_anomalyHistory.Count > 200)
            {
                _anomalyHistory.RemoveAt(0);
            }

            messages.Add(new StreamMessage
            {
                Type = "anomaly_event",
                Seq = _seq,
                Version = Version,
                Timestamp = DateTime.UtcNow,
                GreenhouseId = GreenhouseId,
                Event = anomaly
            });
        }

        return messages;
    }

    private SensorReading CreateNextReading()
    {
        var temperature = Clamp(_current.Temperature + RandomDelta(-0.4, 0.4), 18, 34);
        var humidity = Clamp(_current.Humidity + RandomDelta(-1.5, 1.5), 30, 85);
        var co2 = (int)Clamp(_current.Co2 + RandomDelta(-35, 55), 350, 1250);

        if (_random.NextDouble() < 0.12)
        {
            co2 = _random.Next(920, 1150);
        }

        if (_random.NextDouble() < 0.06)
        {
            temperature = RandomDelta(29, 33);
        }

        if (_random.NextDouble() < 0.06)
        {
            humidity = RandomDelta(30, 38);
        }

        return new SensorReading
        {
            Temperature = Math.Round(temperature, 1),
            Humidity = Math.Round(humidity, 1),
            Co2 = co2
        };
    }

    private AnomalyEvent? DetectAnomaly(SensorReading reading)
    {
        if (reading.Co2 > 900)
        {
            return new AnomalyEvent
            {
                Sensor = "co2",
                Level = reading.Co2 > 1050 ? "critical" : "warning",
                Value = reading.Co2,
                Reason = "CO2 spike z=3.4",
                Message = "CO2 above comfort band — airflow check suggested"
            };
        }

        if (reading.Temperature > 28)
        {
            return new AnomalyEvent
            {
                Sensor = "temperature",
                Level = reading.Temperature > 31 ? "critical" : "warning",
                Value = reading.Temperature,
                Reason = "Temperature outlier",
                Message = "Temperature above safe threshold"
            };
        }

        if (reading.Humidity < 40)
        {
            return new AnomalyEvent
            {
                Sensor = "humidity",
                Level = "warning",
                Value = reading.Humidity,
                Reason = "Humidity dip",
                Message = "Humidity below comfort band"
            };
        }

        return null;
    }

    private string GetStatus()
    {
        if (_current.Co2 > 1050 || _current.Temperature > 31)
        {
            return "ALERT";
        }

        if (_current.Co2 > 900 || _current.Humidity < 40 || _current.Temperature > 28)
        {
            return "WARNING";
        }

        return "LIVE";
    }

    private string GetSummary(SensorReading reading)
    {
        if (reading.Co2 > 1050)
        {
            return "CO2 is high — ventilation recommended";
        }

        if (reading.Temperature > 31)
        {
            return "Temperature is high — cooling recommended";
        }

        if (reading.Humidity < 40)
        {
            return "Humidity is low — misting may be needed";
        }

        if (reading.Co2 > 900)
        {
            return "CO2 rising — monitor airflow";
        }

        return "All sensors operating normally";
    }

    private void GenerateInitialHistory()
    {
        var now = DateTime.UtcNow;

        for (int i = 100; i >= 1; i--)
        {
            _seq++;

            var reading = new SensorReading
            {
                Temperature = Math.Round(23 + Math.Sin(i / 8.0) * 1.8 + RandomDelta(-0.4, 0.4), 1),
                Humidity = Math.Round(62 + Math.Sin(i / 9.0) * 5 + RandomDelta(-1, 1), 1),
                Co2 = 720 + _random.Next(-90, 140)
            };

            _readingHistory.Add(new SensorHistoryItem
            {
                Seq = _seq,
                Timestamp = now.AddSeconds(-i * 10),
                Reading = reading
            });
        }

        _current = _readingHistory.Last().Reading;
    }

    private double RandomDelta(double min, double max)
    {
        return _random.NextDouble() * (max - min) + min;
    }

    private double Clamp(double value, double min, double max)
    {
        return Math.Max(min, Math.Min(max, value));
    }
}