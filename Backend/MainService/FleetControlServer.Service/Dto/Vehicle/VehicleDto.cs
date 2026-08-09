using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace FleetControlServer.Service.DTO.Vehicle;

public class VehicleDto
{
    [StringLength(20)]
    public string ModelName { get; set; } = null!;

    [JsonPropertyName("licensePlate")]
    [StringLength(10)]
    public string? LicensePlateNumber { get; set; }

    // Doubles as the Vehicle's Id — no server-generated Guid is created for vehicles.
    [JsonPropertyName("identNr")]
    [Required]
    [StringLength(40, MinimumLength = 1)]
    public string IdentificationNumber { get; set; } = null!;

    [StringLength(20)]
    public string? Brand { get; set; }

    [Range(1981, 9999)]
    public int? Year { get; set; }

    // Sent as a plain string (e.g. "B", "C1") by the frontend; parsed in the service
    // so an empty/unselected value doesn't blow up JSON deserialization.
    public string? RequiredLicense { get; set; }

    [Range(0, 9999)]
    public int? PowerPs { get; set; }

    [StringLength(20)]
    public string? Color { get; set; }

    public DateOnly? FirstRegistration { get; set; }

    [JsonPropertyName("assignedPersonId")]
    public Guid? VehicleDriverId { get; set; }

    public Guid? TelemetryUnitId { get; set; }

    public VehicleDto() {}
}
