using FleetControlApi.Service.Repositories;
using FleetControlApi.Domain;
using FleetControlApi.Service.Assignments;
using FleetControlApi.Service.Telemetry;

namespace FleetControlApi.Service;

// Behavior for vehicles including assignment to driver and telemetry unit, and position queries
public class VehicleService
{
    private readonly IVehicleRepository _repo;
    private readonly ITelemetryUnitRepository _telemetryUnitRepo;
    private readonly ITripRepository _tripRepo;
    private readonly ITelemetryQueryClient _telemetryQueryClient;
    private readonly IAssignmentPushService _assignmentPushService;

    public VehicleService(
        IVehicleRepository repo,
        ITelemetryUnitRepository telemetryUnitRepo,
        ITripRepository tripRepo,
        ITelemetryQueryClient telemetryQueryClient,
        IAssignmentPushService assignmentPushService
    ) {
        _repo = repo;
        _telemetryUnitRepo = telemetryUnitRepo;
        _tripRepo = tripRepo;
        _telemetryQueryClient = telemetryQueryClient;
        _assignmentPushService = assignmentPushService;
    }

    // Creates a new vehicle if the identification number is valid and does not exist yet
    public async Task<(bool Success, Vehicle? Vehicle, string? Error)> CreateAsync(Vehicle vehicle, Guid? telemetryUnitId)
    {
        var id = vehicle.VehicleIdentificationNumber?.Trim();

        if (string.IsNullOrEmpty(id))
        {
            return (false, null, "Fahrzeug-Identifikationsnummer ist erforderlich.");
        }

        if (id.Length != 17)
        {
            return (false, null, "Die Fahrzeug-Identifikationsnummer muss genau 17 Zeichen lang sein.");
        }

        var existing = await _repo.GetByIdAsync(id);
        if (existing.Success)
        {
            return (false, null, "Ein Fahrzeug mit dieser Identifikationsnummer existiert bereits.");
        }

        return await UpsertAsync(id, vehicle, telemetryUnitId);
    }

    // Updates a vehicle, keeps the telemetry unit assignment consistent and
    // notifies IngestionService about all affected units
    public async Task<(bool Success, Vehicle? Vehicle, string? Error)> UpsertAsync(string id, Vehicle vehicle, Guid? telemetryUnitId)
    {
        if (!string.Equals(id, vehicle.VehicleIdentificationNumber?.Trim(), StringComparison.Ordinal))
        {
            return (false, null, "Die Fahrzeug-Identifikationsnummer kann nicht geändert werden.");
        }

        if (string.IsNullOrWhiteSpace(vehicle.RequiredLicense))
        {
            vehicle.RequiredLicense = null;
        }
        else if (!Enum.TryParse<DriversLicenseType>(vehicle.RequiredLicense, out _))
        {
            return (false, null, "Unbekannter Führerscheintyp.");
        }

        if (vehicle.VehicleDriverId.HasValue && !await _repo.DriverExistsAsync(vehicle.VehicleDriverId.Value))
        {
            return (false, null, "Fahrer nicht gefunden.");
        }

        if (telemetryUnitId.HasValue && !await _telemetryUnitRepo.ExistsAsync(telemetryUnitId.Value))
        {
            return (false, null, "Telemetrieeinheit nicht gefunden.");
        }

        var (hadPrevious, previousVehicle, _) = await _repo.GetByIdAsync(id);
        var previousTelemetryUnitId = hadPrevious ? previousVehicle?.TelemetryUnit?.Id : null;

        // Assignments are only persisted via the foreign keys, never via the navigation properties
        var entity = vehicle;
        entity.VehicleIdentificationNumber = id;
        entity.VehicleDriver = null;
        entity.TelemetryUnit = null;

        var (success, error) = await _repo.UpsertAsync(entity);

        if (!success)
        {
            return (false, null, error);
        }

        if (telemetryUnitId.HasValue)
        {
            await _telemetryUnitRepo.SetVehicleAsync(telemetryUnitId.Value, entity.VehicleIdentificationNumber);
        }
        else
        {
            await _telemetryUnitRepo.ClearVehicleAsync(entity.VehicleIdentificationNumber);
        }

        // IngestionService picks up every changed assignment immediately,
        // instead of only at its next own reload.
        var unitsToNotify = new HashSet<Guid>();
        if (telemetryUnitId.HasValue) unitsToNotify.Add(telemetryUnitId.Value);
        if (previousTelemetryUnitId.HasValue) unitsToNotify.Add(previousTelemetryUnitId.Value);
        await _assignmentPushService.PushAsync(unitsToNotify);

        // entity's TelemetryUnit navigation property was never populated above —
        // the FK update happened on the TelemetryUnit row, not on this instance.
        // Re-fetch so the response reflects the assignment that was just persisted.
        var (found, refreshed, fetchError) = await _repo.GetByIdAsync(entity.VehicleIdentificationNumber);

        if (!found)
        {
            return (false, null, fetchError);
        }

        return (true, refreshed, null);
    }

    // Returns all existing vehicles
    public async Task<IEnumerable<Vehicle>> GetAllAsync()
    {
        return await _repo.GetAllAsync();
    }

    // Returns the vehicle with the given id, or an error message
    public async Task<(bool Success, Vehicle? Vehicle, string? Error)> GetByIdAsync(string id)
    {
        return await _repo.GetByIdAsync(id);
    }

    // Returns the last known telemetry point of all vehicles, by vehicle id
    public async Task<Dictionary<string, TelemetryPoint>> GetLastPositionsAsync()
    {
        var vehicles = await _repo.GetAllAsync();

        var vehicleIds = vehicles.Select(v => v.VehicleIdentificationNumber).ToList();

        return await _telemetryQueryClient.GetLatestPointsByVehicleAsync(vehicleIds);
    }

    // Deletes the vehicle if no trips exist for it
    public async Task<(bool Success, string? Error)> DeleteAsync(string id)
    {
        if (await _tripRepo.ExistsForVehicleAsync(id))
        {
            return (false, "Nicht löschbar solange es Fahrten zu diesem Fahrzeug gibt.");
        }

        Vehicle vehicle = new Vehicle()
        {
            VehicleIdentificationNumber = id,
        };
        await _repo.DeleteAsync(vehicle);

        return (true, null);
    }
}
