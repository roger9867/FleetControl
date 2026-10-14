namespace FleetControlApi.Gateway.SignalR;

// Live update sent to the VehicleHub
public record VehicleUpdateMessage(
    string VehicleId,
    string TelemetryUnitId,
    double Lat,
    double Lng,
    double SpeedKmh,
    double AccelMs2,
    DateTime Timestamp,
    string? DriverId = null);
