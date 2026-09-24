namespace FleetControlServer.Api.Realtime;

// Nachrichtenformat für Fahrtstart
public record TripStartedMessage(
    string TripId,
    string? VehicleId,
    string TelemetryUnitId,
    Guid? DriverId,
    DateTime StartTimestamp);
