using System.ComponentModel.DataAnnotations;

namespace FleetControlServer.Service.DTO.Person;

// Einzelner Führerschein
public class DrivingLicenseDto
{
    public string LicenseClass { get; set; } = null!;

    public DateOnly ObtainedDate { get; set; }
}

// Übertragungsobjekt für Person
public class PersonDto
{
    [StringLength(50)]
    public string FirstName { get; set; } = null!;

    [StringLength(50)]
    public string LastName { get; set; } = null!;

    public DateOnly BirthDate { get; set; }

    public List<DrivingLicenseDto> Licenses { get; set; } = new();

    public string? AssignedVehicleId { get; set; }
}
