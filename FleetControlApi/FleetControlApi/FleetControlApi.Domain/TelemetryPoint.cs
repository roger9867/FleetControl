namespace FleetControlApi.Domain;

// A single telemetry point recorded by a telemetry unit
public record TelemetryPoint(
    DateTime Timestamp,
    double Lat,
    double Lon,
    double SpeedKmh,
    double AccelMs2,
    string? DriverId = null,
    string? DeviceId = null);
