using System.ComponentModel.DataAnnotations;

namespace FleetControlServer.Domain;

public class Trip
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid TelemetryUnitId { get; set; }
    public TelemetryUnit? TelemetryUnit { get; set; }

    // Snapshots of the TelemetryUnit's Vehicle and that Vehicle's driver at the
    // moment the trip started. Captured once, never re-derived — so this trip
    // keeps showing who was actually driving even if the vehicle is later
    // reassigned to someone else or unplugged from this telemetry unit.
    [MaxLength(40)]
    public string? VehicleId { get; set; }

    public Guid? DriverId { get; set; }

    [Required]
    public DateTime StartTimestamp { get; set; }

    public DateTime? EndTimestamp { get; set; }

    public Trip() {}
}
