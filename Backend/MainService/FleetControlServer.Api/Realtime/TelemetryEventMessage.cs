namespace FleetControlServer.Api.Realtime;

// Eigene Kopie des von IngestionService/RabbitMqPublisher veroeffentlichten
// Nachrichtenformats (Projekt-Konvention: keine geteilten Message-Typen
// ueber Service-Grenzen hinweg). Nur die hier tatsaechlich benoetigten
// Felder werden ausgewertet.
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
