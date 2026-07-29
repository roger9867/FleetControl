namespace IngestionService.Models;

public class TelemetryEvent
{
    public string DeviceId { get; set; } = "";

    public double Value { get; set; }

    public DateTime Timestamp { get; set; }
}
