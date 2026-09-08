namespace FleetControlServer.Api.Realtime;

public record TripStartedMessage(
    string TripId,
    string? VehicleId,
    string TelemetryUnitId,
    Guid? DriverId,
    DateTime StartTimestamp);
