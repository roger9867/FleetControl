using System.ComponentModel.DataAnnotations;

namespace FleetControlServer.Domain;

// Eine einzelne Fahrt einer Telemetrie-Einheit mit Start-/Endzeitpunkt und einer
// Momentaufnahme von Fahrzeug und Fahrer zum Fahrtbeginn
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

    public Trip() {}
}
