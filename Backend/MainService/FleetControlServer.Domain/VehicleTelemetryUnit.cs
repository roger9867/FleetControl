namespace FleetControlServer.Domain;

public class VehicleTelemetryUnit
{
    public Guid Id { get; set; } =  Guid.Empty;
    //public bool IsAssigned { get; set; } = false;
    
    public Guid? VehicleId { get; set; }
    public Vehicle? Vehicle { get; set; }

    //public List<VehicleDriver>? AssignedVehicleDrivers { get; private set; } = new();
    
    public VehicleTelemetryUnit() {}
}
