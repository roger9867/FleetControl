using FleetControlApi.Domain;

namespace FleetControlApi.Api.Dto;

// A single telemetry point within a trip
public class TripPointDto
{
    public DateTime Timestamp { get; set; }
    public double Lat { get; set; }
    public double Lng { get; set; }
    public double SpeedKmh { get; set; }
    public double AccelMs2 { get; set; }
}

// Transfer object of a trip with telemetry points
public class TripDto
{
    public Guid Id { get; set; }
    public string? VehicleId { get; set; }
    public Guid TelemetryUnitId { get; set; }
    public Guid? DriverId { get; set; }
    public DateTime Start { get; set; }
    public DateTime? End { get; set; }
    public List<TripPointDto> Points { get; set; } = new();

    // Creates the DTO from a domain trip including its telemetry points
    public static TripDto FromDomain(Trip trip)
    {
        return new TripDto
        {
            Id = trip.Id,
            VehicleId = trip.VehicleId,
            TelemetryUnitId = trip.TelemetryUnitId,
            DriverId = trip.DriverId,
            Start = trip.StartTimestamp,
            End = trip.EndTimestamp,
            Points = trip.Points.Select(p => new TripPointDto
            {
                Timestamp = p.Timestamp,
                Lat = p.Lat,
                Lng = p.Lon,
                SpeedKmh = p.SpeedKmh,
                AccelMs2 = p.AccelMs2
            }).ToList()
        };
    }
}

// A page of trips
public class TripPageDto
{
    public List<TripDto> Trips { get; set; } = new();
    public int TotalCount { get; set; }
}
