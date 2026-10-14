namespace FleetControlApi.Gateway.SignalR;

// Message format for trip start
public record TripStartedMessage(
    string TripId,
    string? VehicleId,
    string TelemetryUnitId,
    Guid? DriverId,
    DateTime StartTimestamp);
