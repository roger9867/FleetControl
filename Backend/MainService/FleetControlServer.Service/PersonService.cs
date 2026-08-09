using FleetControlServer.Data.Repos;
using FleetControlServer.Domain;
using FleetControlServer.Service.Assignments;
using FleetControlServer.Service.DTO.Person;

namespace FleetControlServer.Service;

public class PersonService
{
    private readonly IVehicleDriverRepository _repo;
    private readonly IVehicleRepository _vehicleRepo;
    private readonly ITripRepository _tripRepo;
    private readonly AssignmentPushService _assignmentPushService;

    public PersonService(
        IVehicleDriverRepository repo,
        IVehicleRepository vehicleRepo,
        ITripRepository tripRepo,
        AssignmentPushService assignmentPushService)
    {
        _repo = repo;
        _vehicleRepo = vehicleRepo;
        _tripRepo = tripRepo;
        _assignmentPushService = assignmentPushService;
    }

    public async Task<(bool Success, VehicleDriver? Person, string? Error)> UpsertAsync(Guid id, PersonDto dto)
    {
        var licenses = new List<DriversLicense>();
        foreach (var licenseDto in dto.Licenses ?? new List<DrivingLicenseDto>())
        {
            if (!Enum.TryParse(licenseDto.LicenseClass, out DriversLicenseType parsedType))
            {
                return (false, null, $"Unknown drivers license type: {licenseDto.LicenseClass}");
            }

            licenses.Add(new DriversLicense
            {
                VehicleDriverId = id,
                LicenseType = parsedType,
                ObtainedDate = licenseDto.ObtainedDate
            });
        }

        // Vehicle assignment is optional, but if given it must exist.
        if (!string.IsNullOrEmpty(dto.AssignedVehicleId))
        {
            var vehicleResult = await _vehicleRepo.GetByIdAsync(dto.AssignedVehicleId);

            if (!vehicleResult.Success)
            {
                return (false, null, "Assigned vehicle not found.");
            }
        }

        var entity = new VehicleDriver
        {
            Id = id,
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            DateOfBirth = dto.BirthDate,
            Licenses = licenses
        };

        var (success, error) = await _repo.UpsertAsync(entity);

        if (!success)
        {
            return (false, null, error);
        }

        // Vor der Aenderung merken, welche Fahrzeuge diesem Fahrer bisher
        // zugeordnet waren - deren T-Einheiten (falls vorhanden) muessen bei
        // IngestionService ebenfalls den jetzt entfernten Fahrer los werden.
        var previouslyAssignedVehicles = (await _vehicleRepo.GetAllAsync())
            .Where(v => v.VehicleDriverId == entity.Id && v.Id != dto.AssignedVehicleId)
            .ToList();

        // Keep the Vehicle <-> Driver assignment in sync from this side too:
        // unlink any vehicle that used to point at this driver but shouldn't anymore,
        // then link the newly assigned one (if any).
        await _vehicleRepo.ClearDriverFromOtherVehiclesAsync(entity.Id, dto.AssignedVehicleId);

        if (!string.IsNullOrEmpty(dto.AssignedVehicleId))
        {
            await _vehicleRepo.SetDriverAsync(dto.AssignedVehicleId, entity.Id);
        }

        // Best-effort: IngestionService bekommt die geaenderte Fahrer-
        // Zuordnung sofort mit, statt erst beim naechsten eigenen Reload.
        var unitsToNotify = previouslyAssignedVehicles
            .Where(v => v.TelemetryUnit != null)
            .Select(v => v.TelemetryUnit!.Id)
            .ToHashSet();

        if (!string.IsNullOrEmpty(dto.AssignedVehicleId))
        {
            var assignedVehicleResult = await _vehicleRepo.GetByIdAsync(dto.AssignedVehicleId);
            if (assignedVehicleResult.Vehicle?.TelemetryUnit != null)
            {
                unitsToNotify.Add(assignedVehicleResult.Vehicle.TelemetryUnit.Id);
            }
        }

        await _assignmentPushService.PushAsync(unitsToNotify);

        return (true, entity, null);
    }

    public async Task<IEnumerable<VehicleDriver>> GetAllAsync()
    {
        return await _repo.GetAllAsync();
    }

    public async Task<(bool Success, VehicleDriver? Person, string? Error)> GetByIdAsync(Guid id)
    {
        return await _repo.GetByIdAsync(id);
    }

    public async Task<(bool Success, string? Error)> DeleteAsync(Guid id)
    {
        if (await _tripRepo.ExistsForDriverAsync(id))
        {
            return (false, "Nicht löschbar solange es Fahrten zu dieser Person gibt.");
        }

        var affectedUnits = (await _vehicleRepo.GetAllAsync())
            .Where(v => v.VehicleDriverId == id && v.TelemetryUnit != null)
            .Select(v => v.TelemetryUnit!.Id)
            .ToHashSet();

        await _vehicleRepo.ClearDriverFromOtherVehiclesAsync(id, null);
        await _repo.DeleteAsync(id);

        await _assignmentPushService.PushAsync(affectedUnits);

        return (true, null);
    }
}
