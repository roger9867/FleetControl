using FleetControlServer.Domain;

namespace FleetControlServer.Data.Repos;

public interface IVehicleRepository
{
    public Task<IEnumerable<Vehicle>> GetAllAsync();
    
    public Task<(bool Success, string? Error)> CreateAsync(Vehicle entity);
    
    //public Task UpdateAsync(Vehicle entity);
    
    public Task DeleteAsync(Vehicle entity);
    
    public Task<(bool Success, Vehicle? Vehicle, string? Error)> GetByIdAsync(Guid id);
}