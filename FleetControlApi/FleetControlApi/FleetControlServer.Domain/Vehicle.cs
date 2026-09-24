using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FleetControlServer.Domain;

// Ein Fahrzeug mit Stammdaten, optionalem Fahrer und optionaler Telemetrie-Einheit
public class Vehicle
{
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

    public TelemetryUnit? TelemetryUnit { get; set; }

    public Vehicle() {}
}
