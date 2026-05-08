using FleetControlServer.Domain;
using FleetControlServer.Infra;
using FleetControlServer.Data.Repos;
using FleetControlServer.Service.Dto.TelemetryUnit;


namespace FleetControlServer.Service;

public class TelemetryUnitService
{
    private readonly IUsbVehicleTelemetryUnit _usbTelemetryUnit;
    private readonly ITelemetryUnitRepository _repository;

    public TelemetryUnitService(
        IUsbVehicleTelemetryUnit usbTelemetryUnit, 
        ITelemetryUnitRepository repository
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

    public async Task<bool> CreateAsync(TelemetryUnitDto dto)
    {

        var exists = await _repository.ExistsAsync(dto.Id);

        if (exists)
        {
            return false;
        }

        var entity = new TelemetryUnit
        {
            Id = dto.Id,
            VehicleId = dto.VehicleId,
        };

        await _repository.CreateAsync(entity);

        return true;
    }

    public async Task<List<TelemetryUnitDto>> GetAllAsync()
    {
        List<TelemetryUnit> allTelemetryUnits = _repository.GetAllAsync().Result;

        List<TelemetryUnitDto> allTelemetryUnitDtos 
            = allTelemetryUnits.Select(unit => new TelemetryUnitDto()
                {
                    Id = unit.Id
                }).ToList();

        return allTelemetryUnitDtos;
    }

    public async Task<bool> DeleteAsync(string id)
    {
        if (!Guid.TryParse(id, out Guid guid))
        {
            return false;
        }
        
        return await _repository.DeleteAsync(guid);
    }
}
