using FleetControlServer.Domain;
using FleetControlServer.Infra;
using FleetControlServer.Data.Repos;
using FleetControlServer.Service.Assignments;
using FleetControlServer.Service.Dto.TelemetryUnit;


namespace FleetControlServer.Service;

public class TelemetryUnitService
{
    private readonly IUsbVehicleTelemetryUnit _usbTelemetryUnit;
    private readonly ITelemetryUnitRepository _repository;
    private readonly ITripRepository _tripRepository;
    private readonly AssignmentPushService _assignmentPushService;

    public TelemetryUnitService(
        IUsbVehicleTelemetryUnit usbTelemetryUnit,
        ITelemetryUnitRepository repository,
        ITripRepository tripRepository,
        AssignmentPushService assignmentPushService
        ) {
        _usbTelemetryUnit = usbTelemetryUnit;
        _repository =  repository;
        _tripRepository = tripRepository;
        _assignmentPushService = assignmentPushService;
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

    public async Task<bool> UpdateAsync(Guid id, TelemetryUnitDto dto)
    {
        var exists = await _repository.ExistsAsync(id);

        if (!exists)
        {
            return false;
        }

        Guid? displacedUnitId = null;

        if (!string.IsNullOrEmpty(dto.VehicleId))
        {
            // A vehicle may only have one telemetry unit — unlink whichever
            // unit currently holds it before assigning it to this one.
            var allUnits = await _repository.GetAllAsync();
            displacedUnitId = allUnits
                .FirstOrDefault(u => u.VehicleId == dto.VehicleId && u.Id != id)?.Id;

            await _repository.ClearVehicleAsync(dto.VehicleId);
        }

        var success = await _repository.SetVehicleAsync(id, dto.VehicleId);

        if (success)
        {
            await _assignmentPushService.PushAsync(id);

            // Die verdraengte Einheit zeigt in IngestionServices Cache sonst
            // weiter faelschlich auf dieses Fahrzeug.
            if (displacedUnitId.HasValue)
            {
                await _assignmentPushService.PushAsync(displacedUnitId.Value);
            }
        }

        return success;
    }

    public async Task<List<TelemetryUnitDto>> GetAllAsync()
    {
        List<TelemetryUnit> allTelemetryUnits = _repository.GetAllAsync().Result;

        List<TelemetryUnitDto> allTelemetryUnitDtos
            = allTelemetryUnits.Select(unit => new TelemetryUnitDto()
                {
                    Id = unit.Id,
                    VehicleId = unit.VehicleId
                }).ToList();

        return allTelemetryUnitDtos;
    }

    public async Task<(bool Success, string? Error)> DeleteAsync(string id)
    {
        if (!Guid.TryParse(id, out Guid guid))
        {
            return (false, "Invalid telemetry unit id.");
        }

        if (await _tripRepository.ExistsForTelemetryUnitAsync(guid))
        {
            return (false, "Nicht löschbar solange es Fahrten zu dieser T-Einheit gibt.");
        }

        var deleted = await _repository.DeleteAsync(guid);

        return (deleted, deleted ? null : "Telemetry unit not found.");
    }

    public async Task<List<TelemetryAssignment>> GetCurrentAssignmentsAsync()
    {
        return await _repository.GetCurrentAssignmentsAsync();
    }
}
