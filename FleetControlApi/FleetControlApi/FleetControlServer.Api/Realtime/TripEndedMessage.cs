namespace FleetControlServer.Api.Realtime;

// Nachrichtenformat Fahrtbeendigung
public record TripEndedMessage(
    string TripId,
    string? VehicleId,
    DateTime EndTimestamp);
