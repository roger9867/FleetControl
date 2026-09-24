using System.ComponentModel.DataAnnotations;

namespace FleetControlServer.Domain;

// Person, die als Fahrer einem Fahrzeug zugeordnet werden kann
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
