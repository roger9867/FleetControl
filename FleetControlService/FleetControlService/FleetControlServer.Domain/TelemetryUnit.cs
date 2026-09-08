using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace FleetControlServer.Domain;

public class TelemetryUnit
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public Guid Id { get; set; }

    [MaxLength(40)]
    public string? VehicleId { get; set; }

    // Ignored to avoid a Vehicle <-> TelemetryUnit JSON cycle; Vehicle already exposes this unit.
    [JsonIgnore]
    public Vehicle? Vehicle { get; set; }
    
    //public List<VehicleDriver>? AssignedVehicleDrivers { get; private set; } = new();
    
    public TelemetryUnit() {}
}
