using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FleetControlServer.Domain;

public class Vehicle
{
    // Caller-supplied VIN, not server-generated — mirrors how TelemetryUnit.Id
    // is a client-supplied device id.
    [Key]
    [MaxLength(17)]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public string VehicleIdentificationNumber { get; set; } = null!;

    [Required]
    [MaxLength(50)]
    public string ModelName { get; set; } = null!;

    [MaxLength(9)]
    public string? LicensePlateNumber { get; set; }

    [MaxLength(50)]
    public string? Brand { get; set; }

    public int? Year { get; set; }

    public string? RequiredLicense { get; set; }

    public int? PowerPs { get; set; }

    [MaxLength(50)]
    public string? Color { get; set; }

    public DateOnly? FirstRegistration { get; set; }

    public Guid? VehicleDriverId { get; set; }
    public VehicleDriver? VehicleDriver { get; set; }

    // Inverse side of TelemetryUnit.VehicleId, no own FK column.
    public TelemetryUnit? TelemetryUnit { get; set; }

    public Vehicle() {}
}
