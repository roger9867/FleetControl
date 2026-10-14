using FleetControlApi.Domain;
namespace FleetControlApi.Api.Dto;

// Transfer object for a telemetry unit for the REST API
public class TelemetryUnitDto
{
    public Guid Id { get; set; } =  Guid.Empty;

    public string? VehicleId { get; set; }
    
    public TelemetryUnitDto() {}

    // Converts the DTO into a domain telemetry unit with the given id
    public TelemetryUnit ToDomain(Guid id)
    {
        return new TelemetryUnit { Id = id, VehicleId = VehicleId };
    }

    // Creates the DTO from a domain telemetry unit
    public static TelemetryUnitDto FromDomain(TelemetryUnit unit)
    {
        return new TelemetryUnitDto { Id = unit.Id, VehicleId = unit.VehicleId };
    }
}
