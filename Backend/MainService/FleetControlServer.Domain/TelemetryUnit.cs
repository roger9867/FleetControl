using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FleetControlServer.Domain;

public class TelemetryUnit
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public Guid Id { get; set; }
    
    public Guid? VehicleId { get; set; }
    public Vehicle? Vehicle { get; set; }
    
    //public List<VehicleDriver>? AssignedVehicleDrivers { get; private set; } = new();
    
    public TelemetryUnit() {}
}
