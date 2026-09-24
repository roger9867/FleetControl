using FleetControlServer.Domain;

namespace FleetControlServer.Service.Dto.TelemetryUnit;

// Übertragungsobjekt für eine Telemetrie-Einheit für REST-API
public class TelemetryUnitDto
{
    public Guid Id { get; set; } =  Guid.Empty;

    public string? VehicleId { get; set; }
    
    public TelemetryUnitDto() {}
}
