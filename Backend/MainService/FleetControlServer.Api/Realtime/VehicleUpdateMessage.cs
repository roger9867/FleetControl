namespace FleetControlServer.Api.Realtime;

// An den VehicleHub gesendetes Live-Update - deckt sowohl den Puls-Effekt
// (Fahrzeuge-Seite, nur VehicleId ausgewertet) als auch das Nachzeichnen
// laufender Routen (Fahrten-Seite, volle Position) ab. SignalRs Standard-
// JSON-Protokoll serialisiert die Properties automatisch camelCase.
public record VehicleUpdateMessage(
    string VehicleId,
    string TelemetryUnitId,
    double Lat,
    double Lng,
    double SpeedKmh,
    double AccelMs2,
    DateTime Timestamp);
