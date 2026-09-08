using System.ComponentModel.DataAnnotations;

namespace FleetControlServer.Domain;

// Backs the "Person" concept on the frontend's Personen page. Originally a
// SystemUser subclass with login credentials (email/password) — the Personen UI
// currently only supplies name/employee number/birth date/licenses, so the
// account-related fields are commented out below rather than removed, to be
// reinstated once real user accounts are built.
public class VehicleDriver // : SystemUser
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [MaxLength(50)]
    public string FirstName { get; set; } = null!;

    [Required]
    [MaxLength(50)]
    public string LastName { get; set; } = null!;

    // The frontend's "Mitarbeiter-Nr." is the server-generated Id itself — there is
    // no separate employee number field to enter or edit.

    // [Required]
    // public string Email { get; set; } = null!;
    //
    // [Required]
    // public string PasswordHash { get; set; } = null!;

    [Required]
    public DateOnly DateOfBirth { get; set; }

    public List<DriversLicense> Licenses { get; set; } = new();

    public VehicleDriver() {}

    // Original SystemUser-based constructor, parked alongside the commented-out
    // fields above:
    //
    // public VehicleDriver(
    //     string firstName,
    //     string lastName,
    //     string email,
    //     string passwordHash,
    //     DateOnly dateOfBirth,
    //     List<DriversLicense> driversLicenses)
    // : base(firstName, lastName, email, passwordHash)
    // {
    //     DateOfBirth = dateOfBirth;
    //     if (driversLicenses != null)
    //         DriversLicenses = driversLicenses;
    // }
}
