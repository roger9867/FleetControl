using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FleetControlApi.Domain;

// A single trip of a telemetry unit with start/end time and a
// snapshot of vehicle and driver at the start of the trip
public class Trip
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid TelemetryUnitId { get; set; }
    public TelemetryUnit? TelemetryUnit { get; set; }

    [MaxLength(17)]
    public string? VehicleId { get; set; }

    public Guid? DriverId { get; set; }

    [Required]
    public DateTime StartTimestamp { get; set; }

    public DateTime? EndTimestamp { get; set; }

    // Telemetry points of the trip, loaded from TelemetryDataService (not stored in the database)
    [NotMapped]
    public List<TelemetryPoint> Points { get; set; } = new();

    public Trip() {}
}
