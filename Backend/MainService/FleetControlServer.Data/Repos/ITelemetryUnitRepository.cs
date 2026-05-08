using FleetControlServer.Domain;

namespace FleetControlServer.Data.Repos;

public interface ITelemetryUnitRepository
{
    Task CreateAsync(TelemetryUnit entity);

    Task<List<TelemetryUnit>> GetAllAsync();
    
    Task<bool> ExistsAsync(Guid id);

    Task<bool> DeleteAsync(Guid id);
}
