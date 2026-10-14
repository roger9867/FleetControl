using FleetControlApi.Domain;
using FleetControlApi.Service.Repositories;
using FleetControlApi.Service.Assignments;

namespace FleetControlApi.Service;

// Behavior for telemetry units and their assignment to vehicles
public class TelemetryUnitService
{
    private readonly ITelemetryUnitRepository _repository;
    private readonly ITripRepository _tripRepository;
    private readonly IAssignmentPushService _assignmentPushService;

    public TelemetryUnitService(
        ITelemetryUnitRepository repository,
        ITripRepository tripRepository,
        IAssignmentPushService assignmentPushService
        ) {
        _repository =  repository;
        _tripRepository = tripRepository;
        _assignmentPushService = assignmentPushService;
    }


    // Creates a new telemetry unit if none with this id exists yet
    public async Task<bool> CreateAsync(TelemetryUnit unit)
    {
        var exists = await _repository.ExistsAsync(unit.Id);

        if (exists)
        {
            return false;
        }

        await _repository.CreateAsync(unit);

        return true;
    }

    // Updates the vehicle assignment of a telemetry unit
    public async Task<bool> UpdateAsync(TelemetryUnit unit)
    {
        var id = unit.Id;
        var exists = await _repository.ExistsAsync(id);

        if (!exists)
        {
            return false;
        }

        Guid? displacedUnitId = null;

        if (!string.IsNullOrEmpty(unit.VehicleId))
        {
            var allUnits = await _repository.GetAllAsync();
            displacedUnitId = allUnits
                .FirstOrDefault(u => u.VehicleId == unit.VehicleId && u.Id != id)?.Id;

            await _repository.ClearVehicleAsync(unit.VehicleId);
        }

        var success = await _repository.SetVehicleAsync(id, unit.VehicleId);

        if (success)
        {
            await _assignmentPushService.PushAsync(id);

            if (displacedUnitId.HasValue)
            {
                await _assignmentPushService.PushAsync(displacedUnitId.Value);
            }
        }

        return success;
    }

    // Returns all existing telemetry units including their assigned vehicle
    public async Task<List<TelemetryUnit>> GetAllAsync()
    {
        return await _repository.GetAllAsync();
    }

    // Deletes the telemetry unit if no trips exist for it
    public async Task<(bool Success, string? Error)> DeleteAsync(string id)
    {
        if (!Guid.TryParse(id, out Guid guid))
        {
            return (false, "Ungültige Telemetrieeinheiten-ID.");
        }

        if (await _tripRepository.ExistsForTelemetryUnitAsync(guid))
        {
            return (false, "Nicht löschbar solange es Fahrten zu dieser T-Einheit gibt.");
        }

        var deleted = await _repository.DeleteAsync(guid);

        return (deleted, deleted ? null : "Telemetrieeinheit nicht gefunden.");
    }

}
