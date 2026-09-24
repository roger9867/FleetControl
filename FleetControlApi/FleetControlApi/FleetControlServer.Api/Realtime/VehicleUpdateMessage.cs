namespace FleetControlServer.Api.Realtime;

// An den VehicleHub gesendetes Live-Update
public record VehicleUpdateMessage(
    string VehicleId,
    string TelemetryUnitId,
    double Lat,
    double Lng,
    double SpeedKmh,
    double AccelMs2,
    DateTime Timestamp,
    string? DriverId = null);
