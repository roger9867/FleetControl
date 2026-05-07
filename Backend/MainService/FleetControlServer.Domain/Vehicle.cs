using System.ComponentModel.DataAnnotations;

namespace FleetControlServer.Domain;

public class Vehicle
{
    [Key]
    public Guid Id { get; private set; } = Guid.NewGuid();
    
    //public Guid? LicenseNeededToDriveId { get; set; }

    //public DriversLicense? LicenseNeededToDrive { get; set; }

    [Required]
    public string ModelName { get; private set; } = null!;
    
    [Required]
    public string IdentificationNumber { get; private set; }  = null!;
    
    [Required]
    public string LicensePlateNumber { get; private set; }  = null!;
    
    public Guid? VehicleDriverId { get; set; }
    public VehicleDriver? VehicleDriver { get; set; }
    
    public Vehicle() {}

    public Vehicle(
        //DriversLicense licenseNeededToDrive,
        VehicleDriver vehicleDriver,
        string modelName,
        string identificationNumber,
        string licensePlateNumber)
    {
        //LicenseNeededToDrive = licenseNeededToDrive;
        ModelName = modelName;
        IdentificationNumber = identificationNumber;
        LicensePlateNumber = licensePlateNumber;
        VehicleDriverId = vehicleDriver.Id;
        VehicleDriver = vehicleDriver;
    }
}
