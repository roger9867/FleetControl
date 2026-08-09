namespace FleetControlServer.Data.Repos;

public record TelemetryAssignment(
    Guid TelemetryUnitId,
    string? VehicleId,
    string? LicensePlateNumber,
    Guid? DriverId);
