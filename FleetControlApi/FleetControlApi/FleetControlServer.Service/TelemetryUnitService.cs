using FleetControlServer.Domain;
// using FleetControlServer.Infra;
using FleetControlServer.Data.Repos;
using FleetControlServer.Service.Assignments;
using FleetControlServer.Service.Dto.TelemetryUnit;


namespace FleetControlServer.Service;

// Verhalten für Telemetrie-Einheiten und deren Zuordnung zu Fahrzeugen
public class TelemetryUnitService
{
    // private readonly IUsbVehicleTelemetryUnit _usbTelemetryUnit;
    private readonly ITelemetryUnitRepository _repository;
    private readonly ITripRepository _tripRepository;
    private readonly AssignmentPushService _assignmentPushService;

    public TelemetryUnitService(
        // IUsbVehicleTelemetryUnit usbTelemetryUnit,
        ITelemetryUnitRepository repository,
        ITripRepository tripRepository,
        AssignmentPushService assignmentPushService
        ) {
        // _usbTelemetryUnit = usbTelemetryUnit;
        _repository =  repository;
        _tripRepository = tripRepository;
        _assignmentPushService = assignmentPushService;
    }


    // Legt eine neue Telemetrie-Einheit an, sofern noch keine mit dieser Id existiert
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

    // Aktualisiert die Fahrzeug-Zuordnung einer Telemetrie-Einheit
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
            var allUnits = await _repository.GetAllAsync();
            displacedUnitId = allUnits
                .FirstOrDefault(u => u.VehicleId == dto.VehicleId && u.Id != id)?.Id;

            await _repository.ClearVehicleAsync(dto.VehicleId);
        }

        var success = await _repository.SetVehicleAsync(id, dto.VehicleId);

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

    // Liefert alle vorhandenen Telemetrie-Einheiten als DTOs.
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

    // Löscht die Telemetrie-Einheit, sofern keine Fahrten für sie existieren
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

    // Liefert die aktuellen Zuordnungen aller Telemetrie-Einheiten
    public async Task<List<TelemetryAssignment>> GetCurrentAssignmentsAsync()
    {
        return await _repository.GetCurrentAssignmentsAsync();
    }
}
