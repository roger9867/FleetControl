using FleetControlServer.Domain;

namespace FleetControlServer.Data.Repos;

public interface IVehicleTelemetryUnitRepository
{
    Task CreateAsync(VehicleTelemetryUnit entity);

    Task<List<VehicleTelemetryUnit>> GetAllAsync();
    
    Task<bool> ExistsAsync(Guid id);
}
