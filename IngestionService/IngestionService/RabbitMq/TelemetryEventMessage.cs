namespace IngestionService.RabbitMq;

// Event-Payload für Live-Karten-Updates des UI
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
