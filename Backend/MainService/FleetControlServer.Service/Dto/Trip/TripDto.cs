namespace FleetControlServer.Service.DTO.Trip;

public class TripPointDto
{
    public DateTime Timestamp { get; set; }
    public double Lat { get; set; }
    public double Lng { get; set; }
    public double SpeedKmh { get; set; }
    public double AccelMs2 { get; set; }
}

public class TripDto
{
    public Guid Id { get; set; }
    public string? VehicleId { get; set; }
    public Guid TelemetryUnitId { get; set; }
    public Guid? DriverId { get; set; }
    public DateTime Start { get; set; }
    public DateTime? End { get; set; }
    public List<TripPointDto> Points { get; set; } = new();
}

public class TripPageDto
{
    public List<TripDto> Trips { get; set; } = new();
    public int TotalCount { get; set; }
}
