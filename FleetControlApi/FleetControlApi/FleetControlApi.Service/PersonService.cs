using FleetControlApi.Service.Repositories;
using FleetControlApi.Domain;
using FleetControlApi.Service.Assignments;

namespace FleetControlApi.Service;

// Logic for persons (drivers) including driver's licenses and vehicle assignment
public class PersonService : IPersonService
{
    private readonly IVehicleDriverRepository _repo;
    private readonly IVehicleRepository _vehicleRepo;
    private readonly ITripRepository _tripRepo;
    private readonly IAssignmentPushService _assignmentPushService;

    public PersonService(
        IVehicleDriverRepository repo,
        IVehicleRepository vehicleRepo,
        ITripRepository tripRepo,
        IAssignmentPushService assignmentPushService)
    {
        _repo = repo;
        _vehicleRepo = vehicleRepo;
        _tripRepo = tripRepo;
        _assignmentPushService = assignmentPushService;
    }

    // Creates or updates a person including driver's licenses and keeps the
    // vehicle-driver assignment in sync in both directions
    public async Task<(bool Success, VehicleDriver? Person, string? Error)> UpsertAsync(VehicleDriver person, string? assignedVehicleId)
    {
        // Vehicle is optional, but must exist if specified
        if (!string.IsNullOrEmpty(assignedVehicleId))
        {
            var vehicleResult = await _vehicleRepo.GetByIdAsync(assignedVehicleId);

            if (!vehicleResult.Success)
            {
                return (false, null, "Zugewiesenes Fahrzeug nicht gefunden.");
            }
        }

        foreach (var license in person.Licenses)
        {
            license.VehicleDriverId = person.Id;
        }

        var (success, error) = await _repo.UpsertAsync(person);

        if (!success)
        {
            return (false, null, error);
        }

        // Remember before the change which vehicles were previously assigned to this driver
        var previouslyAssignedVehicles = (await _vehicleRepo.GetAllAsync())
            .Where(v => v.VehicleDriverId == person.Id && v.VehicleIdentificationNumber != assignedVehicleId)
            .ToList();

        await _vehicleRepo.ClearDriverFromOtherVehiclesAsync(person.Id, assignedVehicleId);

        if (!string.IsNullOrEmpty(assignedVehicleId))
        {
            await _vehicleRepo.SetDriverAsync(assignedVehicleId, person.Id);
        }

        var unitsToNotify = previouslyAssignedVehicles
            .Where(v => v.TelemetryUnit != null)
            .Select(v => v.TelemetryUnit!.Id)
            .ToHashSet();

        if (!string.IsNullOrEmpty(assignedVehicleId))
        {
            var assignedVehicleResult = await _vehicleRepo.GetByIdAsync(assignedVehicleId);
            if (assignedVehicleResult.Vehicle?.TelemetryUnit != null)
            {
                unitsToNotify.Add(assignedVehicleResult.Vehicle.TelemetryUnit.Id);
            }
        }

        await _assignmentPushService.PushAsync(unitsToNotify);

        return (true, person, null);
    }

    // Returns all existing persons
    public async Task<IEnumerable<VehicleDriver>> GetAllAsync()
    {
        return await _repo.GetAllAsync();
    }

    // Returns the person with the given id, or an error message
    public async Task<(bool Success, VehicleDriver? Person, string? Error)> GetByIdAsync(Guid id)
    {
        return await _repo.GetByIdAsync(id);
    }

    // Deletes the person if no trips exist for them, and removes them from all vehicle assignments
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
