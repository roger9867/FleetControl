using System.Text.Json.Serialization;

namespace IngestionService.Models;

public class TelemetryEvent
{
    [JsonPropertyName("device_id")]
    public string DeviceId { get; set; } = "";

    [JsonPropertyName("timestamp_iso_8601")]
    public DateTime Timestamp { get; set; }

    [JsonPropertyName("lat")]
    public double Latitude { get; set; }

    [JsonPropertyName("lon")]
    public double Longitude { get; set; }

    [JsonPropertyName("speed_in_kmh")]
    public double SpeedKmh { get; set; }

    [JsonPropertyName("accel_in_ms2")]
    public double AccelerationMs2 { get; set; }
}
