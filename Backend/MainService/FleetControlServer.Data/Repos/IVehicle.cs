using FleetControlServer.Domain;

namespace FleetControlServer.Data.Repos;

public interface IVehicle
{
    public Task<IEnumerable<Vehicle>> GetAllVehicleTelemetryUnitsAsync();
    
    public Task CreateAsync(Vehicle entity);
    
    public Task UpdateAsync(Vehicle entity);
    
    public Task DeleteAsync(Vehicle entity);
    
    public Task<Vehicle> GetVehicleByIdAsync(Guid id);
}