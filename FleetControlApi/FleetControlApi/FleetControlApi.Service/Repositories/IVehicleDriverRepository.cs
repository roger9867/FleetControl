using FleetControlApi.Domain;

namespace FleetControlApi.Service.Repositories;

// Data access for drivers including their driver's licenses
public interface IVehicleDriverRepository
{
    // Returns all existing drivers
    public Task<IEnumerable<VehicleDriver>> GetAllAsync();

    // Creates a new driver or updates an existing one
    public Task<(bool Success, string? Error)> UpsertAsync(VehicleDriver entity);

    // Deletes the driver with the given id
    public Task DeleteAsync(Guid id);

    // Returns the driver with the given id, or an error message
    public Task<(bool Success, VehicleDriver? Person, string? Error)> GetByIdAsync(Guid id);
}
