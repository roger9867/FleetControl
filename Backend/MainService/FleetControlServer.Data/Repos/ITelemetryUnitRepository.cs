using FleetControlServer.Domain;

namespace FleetControlServer.Data.Repos;

public interface ITelemetryUnitRepository
{
    Task CreateAsync(TelemetryUnit entity);

    Task<List<TelemetryUnit>> GetAllAsync();
    
    Task<bool> ExistsAsync(Guid id);

    Task<TelemetryUnit?> GetByIdAsync(Guid id);

    Task<bool> DeleteAsync(Guid id);

    Task<bool> SetVehicleAsync(Guid telemetryUnitId, string? vehicleId);

    Task ClearVehicleAsync(string vehicleId);
}
