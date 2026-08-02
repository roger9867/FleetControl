using System.Text.Json.Serialization;

namespace FleetControlServer.Service.DTO.Vehicle;

public class VehicleDto
{
    public string ModelName { get; set; } = null!;

    [JsonPropertyName("licensePlate")]
    public string? LicensePlateNumber { get; set; }

    [JsonPropertyName("identNr")]
    public string IdentificationNumber { get; set; } = null!;

    public string? Brand { get; set; }

    public int? Year { get; set; }

    // Sent as a plain string (e.g. "B", "C1") by the frontend; parsed in the service
    // so an empty/unselected value doesn't blow up JSON deserialization.
    public string? RequiredLicense { get; set; }

    public int? PowerPs { get; set; }

    public string? Color { get; set; }

    public DateOnly? FirstRegistration { get; set; }

    [JsonPropertyName("assignedPersonId")]
    public Guid? VehicleDriverId { get; set; }

    public Guid? TelemetryUnitId { get; set; }

    public VehicleDto() {}
}
