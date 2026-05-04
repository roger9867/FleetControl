using FleetControlServer.Domain;
using FleetControlServer.Infra;
using FleetControlServer.Data.Repos;


namespace FleetControlServer.Service;

public class VehicleTelemetryUnitService
{
    private readonly IUsbVehicleTelemetryUnit _usbTelemetryUnit;
    private readonly IVehicleTelemetryUnitRepository _repository;

    public VehicleTelemetryUnitService(
        IUsbVehicleTelemetryUnit usbTelemetryUnit, 
        IVehicleTelemetryUnitRepository repository
        ) {
        _usbTelemetryUnit = usbTelemetryUnit;
        _repository =  repository;
    }
    
    // Nachricht an alle angeschlossenen Geräte
    public async Task<Dictionary<string, string?>> BroadcastCommandAsync(string commandMessage)
    {
        var ports = _usbTelemetryUnit.GetAvailablePortNames();

        var tasks = ports.Select(async port =>
        {
            var response = await _usbTelemetryUnit.SendCommandAsync(port, commandMessage);
            return (port, response);
        });
        
        var results = await Task.WhenAll(tasks);
        return results.ToDictionary(r => r.port, r => r.response);
    }

    public async Task<bool> CreateAsync(string id)
    {
        if (!Guid.TryParse(id, out var guid))
        {
            return false;
        }

        var exists = await _repository.ExistsAsync(guid);

        if (exists)
        {
            return false;
        }

        var entity = new VehicleTelemetryUnit
        {
            Id = guid
        };

        await _repository.CreateAsync(entity);

        return true;
    }
}
