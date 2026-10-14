using System.ComponentModel.DataAnnotations;
using FleetControlApi.Domain;

namespace FleetControlApi.Api.Dto;

// Single driver's license
public class DrivingLicenseDto
{
    public string LicenseClass { get; set; } = null!;

    public DateOnly ObtainedDate { get; set; }
}

// Transfer object for a person
public class PersonDto
{
    [StringLength(50)]
    public string FirstName { get; set; } = null!;

    [StringLength(50)]
    public string LastName { get; set; } = null!;

    public DateOnly BirthDate { get; set; }

    public List<DrivingLicenseDto> Licenses { get; set; } = new();

    public string? AssignedVehicleId { get; set; }

    // Converts the DTO into a domain person, fails on unknown license classes
    public bool TryToDomain(Guid id, out VehicleDriver person, out string? error)
    {
        person = new VehicleDriver
        {
            Id = id,
            FirstName = FirstName,
            LastName = LastName,
            DateOfBirth = BirthDate
        };
        error = null;

        foreach (var license in Licenses ?? new List<DrivingLicenseDto>())
        {
            if (!Enum.TryParse(license.LicenseClass, out DriversLicenseType parsedType))
            {
                error = $"Unbekannter Führerscheintyp: {license.LicenseClass}";
                return false;
            }

            person.Licenses.Add(new DriversLicense(parsedType, license.ObtainedDate));
        }

        return true;
    }
}
