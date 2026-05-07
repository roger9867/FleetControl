using FleetControlServer.Domain;

namespace FleetControlServer.Data.Repos;

public interface IVehicleTelemetryUnitRepository
{
    Task CreateAsync(TelemetryUnit entity);

    Task<List<TelemetryUnit>> GetAllAsync();
    
    Task<bool> ExistsAsync(Guid id);

    Task<bool> DeleteAsync(Guid id);
}
