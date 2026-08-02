using System.ComponentModel.DataAnnotations;

namespace FleetControlServer.Domain;

public class Vehicle
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public string ModelName { get; set; } = null!;

    [Required]
    public string IdentificationNumber { get; set; } = null!;

    public string? LicensePlateNumber { get; set; }

    public string? Brand { get; set; }

    public int? Year { get; set; }

    public DriversLicenseType? RequiredLicense { get; set; }

    public int? PowerPs { get; set; }

    public string? Color { get; set; }

    public DateOnly? FirstRegistration { get; set; }

    public Guid? VehicleDriverId { get; set; }
    public VehicleDriver? VehicleDriver { get; set; }

    // Inverse side of TelemetryUnit.VehicleId, no own FK column.
    public TelemetryUnit? TelemetryUnit { get; set; }

    public Vehicle() {}
}
