using FleetControlServer.Domain;

namespace FleetControlServer.Data.Repos;

public interface IVehicleRepository
{
    public Task<IEnumerable<Vehicle>> GetAllAsync();

    public Task<(bool Success, string? Error)> UpsertAsync(Vehicle entity);

    public Task DeleteAsync(Vehicle entity);

    public Task<(bool Success, Vehicle? Vehicle, string? Error)> GetByIdAsync(Guid id);

    public Task<bool> DriverExistsAsync(Guid id);

    public Task<bool> SetDriverAsync(Guid vehicleId, Guid? driverId);

    public Task ClearDriverFromOtherVehiclesAsync(Guid driverId, Guid? exceptVehicleId);
}
