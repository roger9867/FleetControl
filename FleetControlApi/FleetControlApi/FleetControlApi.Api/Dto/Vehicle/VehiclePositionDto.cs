using FleetControlApi.Domain;

namespace FleetControlApi.Api.Dto;

// Last known position and telemetry of a vehicle
public class VehiclePositionDto
{
    public string VehicleId { get; set; } = null!;

    public string TelemetryUnitId { get; set; } = null!;

    public string? DriverId { get; set; }

    public double Lat { get; set; }

    public double Lng { get; set; }

    public double SpeedKmh { get; set; }

    public double AccelMs2 { get; set; }

    public DateTime Timestamp { get; set; }

    public VehiclePositionDto() {}

    // Creates the DTO from the last telemetry point of a vehicle
    public static VehiclePositionDto FromDomain(string vehicleId, TelemetryPoint point)
    {
        return new VehiclePositionDto
        {
            VehicleId = vehicleId,
            TelemetryUnitId = point.DeviceId ?? string.Empty,
            DriverId = point.DriverId,
            Lat = point.Lat,
            Lng = point.Lon,
            SpeedKmh = point.SpeedKmh,
            AccelMs2 = point.AccelMs2,
            Timestamp = point.Timestamp
        };
    }
}
