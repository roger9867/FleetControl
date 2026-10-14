using System.ComponentModel.DataAnnotations;

namespace FleetControlApi.Domain;

// Person who can be assigned to a vehicle as driver
public class VehicleDriver
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [MaxLength(50)]
    public string FirstName { get; set; } = null!;

    [Required]
    [MaxLength(50)]
    public string LastName { get; set; } = null!;

    [Required]
    public DateOnly DateOfBirth { get; set; }

    public List<DriversLicense> Licenses { get; set; } = new();

    public VehicleDriver() {}

}
