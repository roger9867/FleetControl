namespace FleetControlServer.Api.Realtime;

// An den VehicleHub gesendet, sobald IngestionService eine Fahrt per gRPC
// als beendet meldet - erlaubt der Fahrten-Seite, das Pulsieren des letzten
// Punkts sofort zu beenden, ohne neu laden zu muessen.
public record TripEndedMessage(
    string TripId,
    string? VehicleId,
    DateTime EndTimestamp);
