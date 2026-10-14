using FleetControlApi.Domain;

namespace FleetControlApi.Service.Repositories;

// Data access for vehicles
public interface IVehicleRepository
{
    // Returns all existing vehicles
    public Task<IEnumerable<Vehicle>> GetAllAsync();

    // Creates a new vehicle or updates an existing one
    public Task<(bool Success, string? Error)> UpsertAsync(Vehicle entity);

    // Deletes the given vehicle
    public Task DeleteAsync(Vehicle entity);

    // Returns the vehicle with the given id, or an error message
    public Task<(bool Success, Vehicle? Vehicle, string? Error)> GetByIdAsync(string id);

    // Checks whether a driver with the given id exists
    public Task<bool> DriverExistsAsync(Guid id);

    // Sets or removes the assigned driver of a vehicle
    public Task<bool> SetDriverAsync(string vehicleId, Guid? driverId);

    // Removes a driver from all vehicles except the given one, so that a driver is assigned to only one vehicle
    public Task ClearDriverFromOtherVehiclesAsync(Guid driverId, string? exceptVehicleId);
}
