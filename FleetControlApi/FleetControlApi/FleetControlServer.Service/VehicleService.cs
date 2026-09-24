using FleetControlServer.Data.Repos;
using FleetControlServer.Domain;
using FleetControlServer.Service.Assignments;
using FleetControlServer.Service.DTO.Vehicle;
using FleetControlServer.Service.Telemetry;

namespace FleetControlServer.Service;

// Verhalten für Fahrzeuge inklusive Zuordnung zu Fahrer und Telemetrie-Einheit und Positionsabfragen
public class VehicleService
{
    private readonly IVehicleRepository _repo;
    private readonly ITelemetryUnitRepository _telemetryUnitRepo;
    private readonly ITripRepository _tripRepo;
    private readonly TelemetryQueryClient _telemetryQueryClient;
    private readonly AssignmentPushService _assignmentPushService;

    public VehicleService(
        IVehicleRepository repo,
        ITelemetryUnitRepository telemetryUnitRepo,
        ITripRepository tripRepo,
        TelemetryQueryClient telemetryQueryClient,
        AssignmentPushService assignmentPushService)
    {
        _repo = repo;
        _telemetryUnitRepo = telemetryUnitRepo;
        _tripRepo = tripRepo;
        _telemetryQueryClient = telemetryQueryClient;
        _assignmentPushService = assignmentPushService;
    }

    // Legt ein neues Fahrzeug an, sofern die Identifikationsnummer gültig ist und noch nicht existiert
    public async Task<(bool Success, Vehicle? Vehicle, string? Error)> CreateAsync(VehicleDto dto)
    {
        var id = dto.IdentificationNumber?.Trim();

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

        return await UpsertAsync(id, dto);
    }

    // Aktualisiert ein Fahrzeug, hält die Telemetrie-Einheiten-Zuordnung konsistent und
    // benachrichtigt IngestionService über alle betroffenen Einheiten
    public async Task<(bool Success, Vehicle? Vehicle, string? Error)> UpsertAsync(string id, VehicleDto dto)
    {
        if (!string.Equals(id, dto.IdentificationNumber?.Trim(), StringComparison.Ordinal))
        {
            return (false, null, "Die Fahrzeug-Identifikationsnummer kann nicht geändert werden.");
        }

        string? requiredLicense = null;
        if (!string.IsNullOrWhiteSpace(dto.RequiredLicense))
        {
            if (!Enum.TryParse<DriversLicenseType>(dto.RequiredLicense, out _))
            {
                return (false, null, "Unbekannter Führerscheintyp.");
            }

            requiredLicense = dto.RequiredLicense;
        }

        if (dto.VehicleDriverId.HasValue && !await _repo.DriverExistsAsync(dto.VehicleDriverId.Value))
        {
            return (false, null, "Fahrer nicht gefunden.");
        }

        if (dto.TelemetryUnitId.HasValue && !await _telemetryUnitRepo.ExistsAsync(dto.TelemetryUnitId.Value))
        {
            return (false, null, "Telemetrieeinheit nicht gefunden.");
        }

        var (hadPrevious, previousVehicle, _) = await _repo.GetByIdAsync(id);
        var previousTelemetryUnitId = hadPrevious ? previousVehicle?.TelemetryUnit?.Id : null;

        var entity = new Vehicle
        {
            VehicleIdentificationNumber = id,
            LicensePlateNumber = dto.LicensePlateNumber,
            ModelName = dto.ModelName,
            Brand = dto.Brand,
            Year = dto.Year,
            RequiredLicense = requiredLicense,
            PowerPs = dto.PowerPs,
            Color = dto.Color,
            FirstRegistration = dto.FirstRegistration,
            VehicleDriverId = dto.VehicleDriverId,
        };

        var (success, error) = await _repo.UpsertAsync(entity);

        if (!success)
        {
            return (false, null, error);
        }

        if (dto.TelemetryUnitId.HasValue)
        {
            await _telemetryUnitRepo.SetVehicleAsync(dto.TelemetryUnitId.Value, entity.VehicleIdentificationNumber);
        }
        else
        {
            await _telemetryUnitRepo.ClearVehicleAsync(entity.VehicleIdentificationNumber);
        }

        // IngestionService bekommt jede geänderte Zuordnung sofort mit,
        // statt erst beim nächsten eigenen Reload.
        var unitsToNotify = new HashSet<Guid>();
        if (dto.TelemetryUnitId.HasValue) unitsToNotify.Add(dto.TelemetryUnitId.Value);
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

    // Liefert alle vorhandenen Fahrzeuge
    public async Task<IEnumerable<Vehicle>> GetAllAsync()
    {
        return await _repo.GetAllAsync();
    }

    // Liefert das Fahrzeug mit der angegebenen Id oder eine Fehlermeldung
    public async Task<(bool Success, Vehicle? Vehicle, string? Error)> GetByIdAsync(string id)
    {
        return await _repo.GetByIdAsync(id);
    }

    // Liefert die zuletzt bekannte Position und Telemetrie aller Fahrzeuge
    public async Task<List<VehiclePositionDto>> GetLastPositionsAsync()
    {
        var vehicles = await _repo.GetAllAsync();
        var vehicleList = vehicles.ToList();

        var vehicleIds = vehicleList.Select(v => v.VehicleIdentificationNumber).ToList();

        var latestByVehicleId = await _telemetryQueryClient.GetLatestPointsByVehicleAsync(vehicleIds);

        var result = new List<VehiclePositionDto>();

        foreach (var vehicle in vehicleList)
        {
            if (!latestByVehicleId.TryGetValue(vehicle.VehicleIdentificationNumber, out var point))
            {
                continue;
            }

            result.Add(new VehiclePositionDto
            {
                VehicleId = vehicle.VehicleIdentificationNumber,
                TelemetryUnitId = point.DeviceId ?? string.Empty,
                DriverId = point.DriverId,
                Lat = point.Lat,
                Lng = point.Lon,
                SpeedKmh = point.SpeedKmh,
                AccelMs2 = point.AccelMs2,
                Timestamp = point.Timestamp
            });
        }

        return result;
    }

    // Löscht das Fahrzeug, sofern keine Fahrten für es existieren
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
