using System.ComponentModel.DataAnnotations;

namespace FleetControlServer.Service.DTO.Person;

public class DrivingLicenseDto
{
    public string LicenseClass { get; set; } = null!;

    public DateOnly ObtainedDate { get; set; }
}

public class PersonDto
{
    [StringLength(50)]
    public string FirstName { get; set; } = null!;

    [StringLength(50)]
    public string LastName { get; set; } = null!;

    // No employee number field — the server-generated Id doubles as it.

    public DateOnly BirthDate { get; set; }

    public List<DrivingLicenseDto> Licenses { get; set; } = new();

    public string? AssignedVehicleId { get; set; }
}
