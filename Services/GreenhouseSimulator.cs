using GreenhouseGuard.Server.Models;

namespace GreenhouseGuard.Server.Services;

public class GreenhouseSimulator
{
    private readonly Random _random = new();

    private const int Version = 3;
    private const string GreenhouseId = "north-glasshouse-block-a";

    // Configuration for anomaly event patterns
    private const int ReadingsBetweenEvents = 5; // Number of reading_delta messages before triggering an event
    private readonly string[] _eventSequence = { "co2", "temperature", "humidity" }; // Order of events to trigger
    private int _currentEventIndex = 0;
    private int _readingsSinceLastEvent = 0;

    // Sensor ranges
    private const double TemperatureMin = 18;
    private const double TemperatureMax = 28;
    private const double HumidityMin = 45;
    private const double HumidityMax = 75;
    private const int Co2Min = 350;
    private const int Co2Max = 900;

    private long _seq = 101;

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
            History = _readingHistory.TakeLast(20).ToList(),
            Anomalies = _anomalyHistory.TakeLast(7).ToList(),
            Ranges = new SensorRanges
            {
                Temperature = new SensorRange { Min = TemperatureMin, Max = TemperatureMax },
                Humidity = new SensorRange { Min = HumidityMin, Max = HumidityMax },
                Co2 = new SensorRange { Min = Co2Min, Max = Co2Max }
            }
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

        // Increment counter for readings since last event
        _readingsSinceLastEvent++;

        // Check if we should trigger the next event in sequence
        if (_readingsSinceLastEvent >= ReadingsBetweenEvents)
        {
            var nextSensorType = _eventSequence[_currentEventIndex];
            
            // Force reading to have anomalous value matching the event condition
            switch (nextSensorType)
            {
                case "co2":
                    _current.Co2 = _random.Next(910, 1000); // Above warning threshold
                    break;
                case "temperature":
                    _current.Temperature = Math.Round(RandomDelta(29, 32), 1); // Above warning threshold
                    break;
                case "humidity":
                    _current.Humidity = Math.Round(RandomDelta(30, 39), 1); // Below warning threshold
                    break;
            }
            
            var anomaly = DetectAnomalyByType(_current, nextSensorType);

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
                    Event = anomaly,
                    Reading = _current
                });

                // Move to next event in sequence
                _currentEventIndex = (_currentEventIndex + 1) % _eventSequence.Length;
                _readingsSinceLastEvent = 0;
            }
        }

        return messages;
    }

    private SensorReading CreateNextReading()
    {
        var temperature = Clamp(_current.Temperature + RandomDelta(-0.4, 0.4), TemperatureMin, TemperatureMax);
        var humidity = Clamp(_current.Humidity + RandomDelta(-1.5, 1.5), HumidityMin, HumidityMax);
        var co2 = (int)Clamp(_current.Co2 + RandomDelta(-35, 55), Co2Min, Co2Max);

        if (_random.NextDouble() < 0.12)
        {
            co2 = _random.Next(800, 900);
        }

        if (_random.NextDouble() < 0.06)
        {
            temperature = RandomDelta(26, 28);
        }

        if (_random.NextDouble() < 0.06)
        {
            humidity = RandomDelta(45, 55);
        }

        return new SensorReading
        {
            Temperature = Math.Round(temperature, 1),
            Humidity = Math.Round(humidity, 1),
            Co2 = co2
        };
    }

    private AnomalyEvent? DetectAnomalyByType(SensorReading reading, string sensorType)
    {
        switch (sensorType)
        {
            case "co2":
                return new AnomalyEvent
                {
                    Sensor = "co2",
                    Level = "warning",
                    Value = reading.Co2,
                    Reason = "CO2 spike z=3.4",
                    Message = "CO2 above comfort band — airflow check suggested"
                };

            case "temperature":
                return new AnomalyEvent
                {
                    Sensor = "temperature",
                    Level = "warning",
                    Value = reading.Temperature,
                    Reason = "Temperature outlier",
                    Message = "Temperature above safe threshold"
                };

            case "humidity":
                return new AnomalyEvent
                {
                    Sensor = "humidity",
                    Level = "warning",
                    Value = reading.Humidity,
                    Reason = "Humidity dip",
                    Message = "Humidity below comfort band"
                };

            default:
                return null;
        }
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