using FleetControlServer.Domain;

namespace FleetControlServer.Data.Repos;

public interface IVehicleDriverRepository
{
    public Task<IEnumerable<VehicleDriver>> GetAllAsync();

    public Task<(bool Success, string? Error)> UpsertAsync(VehicleDriver entity);

    public Task DeleteAsync(Guid id);

    public Task<(bool Success, VehicleDriver? Person, string? Error)> GetByIdAsync(Guid id);
}
