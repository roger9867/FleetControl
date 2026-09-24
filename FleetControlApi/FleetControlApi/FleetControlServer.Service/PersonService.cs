using FleetControlServer.Data.Repos;
using FleetControlServer.Domain;
using FleetControlServer.Service.Assignments;
using FleetControlServer.Service.DTO.Person;

namespace FleetControlServer.Service;

// Logik für Personen (Fahrer) inklusive Führerscheinen und Fahrzeug-Zuordnung
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

    // Legt eine Person an oder aktualisiert sie inklusive Führerscheinen und hält die
    // Fahrzeug-Fahrer-Zuordnung in beide Richtungen synchron
    public async Task<(bool Success, VehicleDriver? Person, string? Error)> UpsertAsync(Guid id, PersonDto dto)
    {
        var licenses = new List<DriversLicense>();
        foreach (var licenseDto in dto.Licenses ?? new List<DrivingLicenseDto>())
        {
            if (!Enum.TryParse(licenseDto.LicenseClass, out DriversLicenseType parsedType))
            {
                return (false, null, $"Unbekannter Führerscheintyp: {licenseDto.LicenseClass}");
            }

            licenses.Add(new DriversLicense
            {
                VehicleDriverId = id,
                LicenseType = parsedType,
                ObtainedDate = licenseDto.ObtainedDate
            });
        }

        // Fahrzeug is optional, muss aber existieren falls es angegeben wird
        if (!string.IsNullOrEmpty(dto.AssignedVehicleId))
        {
            var vehicleResult = await _vehicleRepo.GetByIdAsync(dto.AssignedVehicleId);

            if (!vehicleResult.Success)
            {
                return (false, null, "Zugewiesenes Fahrzeug nicht gefunden.");
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

        // Vor der Änderung merken, welche Fahrzeuge diesem Fahrer bisher zugeordnet waren
        var previouslyAssignedVehicles = (await _vehicleRepo.GetAllAsync())
            .Where(v => v.VehicleDriverId == entity.Id && v.VehicleIdentificationNumber != dto.AssignedVehicleId)
            .ToList();

        await _vehicleRepo.ClearDriverFromOtherVehiclesAsync(entity.Id, dto.AssignedVehicleId);

        if (!string.IsNullOrEmpty(dto.AssignedVehicleId))
        {
            await _vehicleRepo.SetDriverAsync(dto.AssignedVehicleId, entity.Id);
        }

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

    // Liefert alle vorhandenen Personen
    public async Task<IEnumerable<VehicleDriver>> GetAllAsync()
    {
        return await _repo.GetAllAsync();
    }

    // Liefert die Person mit der angegebenen Id oder eine Fehlermeldung
    public async Task<(bool Success, VehicleDriver? Person, string? Error)> GetByIdAsync(Guid id)
    {
        return await _repo.GetByIdAsync(id);
    }

    // Löscht die Person, sofern keine Fahrten für sie existieren, und entfernt sie aus allen Fahrzeug-Zuordnungen
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
