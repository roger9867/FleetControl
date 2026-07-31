using System.ComponentModel.DataAnnotations;

namespace FleetControlServer.Domain;

public class Trip
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid TelemetryUnitId { get; set; }
    public TelemetryUnit? TelemetryUnit { get; set; }

    [Required]
    public DateTime StartTimestamp { get; set; }

    public DateTime? EndTimestamp { get; set; }

    public Trip() {}
}
