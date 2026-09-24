using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace FleetControlServer.Service.DTO.Vehicle;

// Übertragungsobjekt zum Anlegen/Aktualisieren eines Fahrzeugs
public class VehicleDto
{
    [StringLength(50)]
    public string ModelName { get; set; } = null!;

    [JsonPropertyName("licensePlate")]
    [StringLength(9)]
    public string? LicensePlateNumber { get; set; }

    [JsonPropertyName("identNr")]
    [Required]
    [StringLength(17, MinimumLength = 17)]
    public string IdentificationNumber { get; set; } = null!;

    [StringLength(50)]
    public string? Brand { get; set; }

    [Range(1981, 9999)]
    public int? Year { get; set; }

    public string? RequiredLicense { get; set; }

    [Range(0, 9999)]
    public int? PowerPs { get; set; }

    [StringLength(50)]
    public string? Color { get; set; }

    public DateOnly? FirstRegistration { get; set; }

    [JsonPropertyName("assignedPersonId")]
    public Guid? VehicleDriverId { get; set; }

    public Guid? TelemetryUnitId { get; set; }

    public VehicleDto() {}
}
