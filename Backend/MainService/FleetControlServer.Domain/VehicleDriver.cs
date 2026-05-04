using System.ComponentModel.DataAnnotations;

namespace FleetControlServer.Domain;

public class VehicleDriver : SystemUser
{
    [Key]
    public Guid Id { get; private set; } =  Guid.NewGuid();
    
    [Required]
    public DateOnly DateOfBirth { get; private set; }

    public List<DriversLicense> DriversLicenses = new();


    public VehicleDriver() {}

    public VehicleDriver(
        string firstName,
        string lastName,
        string email,
        string passwordHash,
        DateOnly dateOfBirth,
        List<DriversLicense>  driversLicenses)
    : base(firstName, lastName, email, passwordHash)
    {
        DateOfBirth = dateOfBirth;
        if (driversLicenses != null)
            DriversLicenses = driversLicenses;
    }
}
