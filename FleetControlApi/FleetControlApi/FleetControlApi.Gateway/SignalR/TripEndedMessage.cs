namespace FleetControlApi.Gateway.SignalR;

// Message format for trip end
public record TripEndedMessage(
    string TripId,
    string? VehicleId,
    DateTime EndTimestamp);
