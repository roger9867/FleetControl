using System.ComponentModel.DataAnnotations;

namespace FleetControlServer.Domain;

public class Vehicle
{
    [Key]
    public Guid Id { get; private set; } = Guid.NewGuid();
    
    [Required]
    public DriversLicense LicenseNeededToDrive { get; set; } = null!;

    [Required]
    public string ModelName { get; private set; } = null!;
    
    [Required]
    public string IdentificationNumber { get; private set; }  = null!;
    
    [Required]
    public string LicensePlateNumber { get; private set; }  = null!;
    
    public Vehicle() {}

    public Vehicle(
        DriversLicense licenseNeededToDrive,
        string modelName,
        string identificationNumber,
        string licensePlateNumber)
    {
        LicenseNeededToDrive = licenseNeededToDrive;
        ModelName = modelName;
        IdentificationNumber = identificationNumber;
        LicensePlateNumber = licensePlateNumber;
    }
}
