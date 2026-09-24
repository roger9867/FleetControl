namespace FleetControlServer.Data.Repos;

// Aktuelle Zuordnung einer Telemetrie-Einheit zu Fahrzeug, Kennzeichen und Fahrer
public record TelemetryAssignment(
    Guid TelemetryUnitId,
    string? VehicleId,
    string? LicensePlateNumber,
    Guid? DriverId);
