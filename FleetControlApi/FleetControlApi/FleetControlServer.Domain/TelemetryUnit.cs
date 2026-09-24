using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace FleetControlServer.Domain;

// Eine physische Telemetrie-Einheit, optional einem Fahrzeug zugeordnet.
public class TelemetryUnit
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public Guid Id { get; set; }

    [MaxLength(40)]
    public string? VehicleId { get; set; }

    [JsonIgnore]
    public Vehicle? Vehicle { get; set; }
    
    public TelemetryUnit() {}
}
