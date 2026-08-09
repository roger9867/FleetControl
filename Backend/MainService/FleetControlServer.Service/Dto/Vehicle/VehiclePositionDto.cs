namespace FleetControlServer.Service.DTO.Vehicle;

public class VehiclePositionDto
{
    public string VehicleId { get; set; } = null!;

    public string TelemetryUnitId { get; set; } = null!;

    public double Lat { get; set; }

    public double Lng { get; set; }

    public double SpeedKmh { get; set; }

    public double AccelMs2 { get; set; }

    public DateTime Timestamp { get; set; }

    public VehiclePositionDto() {}
}
