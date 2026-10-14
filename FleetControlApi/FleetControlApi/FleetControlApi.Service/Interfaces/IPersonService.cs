using FleetControlApi.Domain;

namespace FleetControlApi.Service;

// Logic for persons (drivers) including driver's licenses and vehicle assignment
public interface IPersonService
{
    // Creates or updates a person including driver's licenses and keeps the
    // vehicle-driver assignment in sync in both directions
    public Task<(bool Success, VehicleDriver? Person, string? Error)> UpsertAsync(VehicleDriver person, string? assignedVehicleId);

    // Returns all existing persons
    public Task<IEnumerable<VehicleDriver>> GetAllAsync();

    // Returns the person with the given id, or an error message
    public Task<(bool Success, VehicleDriver? Person, string? Error)> GetByIdAsync(Guid id);

    // Deletes the person if no trips exist for them, and removes them from all vehicle assignments
    public Task<(bool Success, string? Error)> DeleteAsync(Guid id);
}
