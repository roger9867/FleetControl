using FleetControlApi.Domain;

namespace FleetControlApi.Service.Repositories;

// Data access for telemetry units and their assignment to vehicles
public interface ITelemetryUnitRepository
{
    // Creates a new telemetry unit
    Task CreateAsync(TelemetryUnit entity);

    // Returns all existing telemetry units including their assigned vehicle
    Task<List<TelemetryUnit>> GetAllAsync();

    // Checks whether a telemetry unit with the given id exists
    Task<bool> ExistsAsync(Guid id);

    // Returns the telemetry unit with the given id including its assigned vehicle, or null
    Task<TelemetryUnit?> GetByIdAsync(Guid id);

    // Deletes the telemetry unit with the given id
    Task<bool> DeleteAsync(Guid id);

    // Sets or removes the vehicle assignment of a telemetry unit
    Task<bool> SetVehicleAsync(Guid telemetryUnitId, string? vehicleId);

    // Removes the vehicle assignment from all telemetry units assigned to the given vehicle
    Task ClearVehicleAsync(string vehicleId);

}
