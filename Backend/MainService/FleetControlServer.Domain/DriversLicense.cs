using System.ComponentModel.DataAnnotations;

namespace FleetControlServer.Domain;

public class DriversLicense
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    
    [Required]
    public DriversLicenseType LicenseType { get; private set; }
    
    [Required]
    public VehicleClass VehicleClass { get; private set; }
    
    public DriversLicense() {}

    public DriversLicense(DriversLicenseType type, VehicleClass vehicleClass)
    {
        LicenseType = type;
        VehicleClass = vehicleClass;
    }
}
