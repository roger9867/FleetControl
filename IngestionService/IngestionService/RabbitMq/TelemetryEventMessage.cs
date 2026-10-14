namespace IngestionService.RabbitMq;

// Event payload for live map updates of the UI
public record TelemetryEventMessage(
    string DeviceId,
    string? VehicleId,
    string? LicensePlate,
    string? DriverId,
    string Topic,
    string State,
    double Lat,
    double Lon,
    double SpeedKmh,
    double AccelMs2,
    DateTime Timestamp);
