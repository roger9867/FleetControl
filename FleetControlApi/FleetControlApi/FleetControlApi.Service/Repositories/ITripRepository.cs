using FleetControlApi.Domain;

namespace FleetControlApi.Service.Repositories;

// Data access for trips
public interface ITripRepository
{
    // Creates a new trip
    Task CreateAsync(Trip entity);

    // Returns the currently open (not yet ended) trip of a telemetry unit, if any
    Task<Trip?> GetOpenTripByTelemetryUnitIdAsync(Guid telemetryUnitId);

    // Returns the trip with the given id, or null
    Task<Trip?> GetByIdAsync(Guid id);

    // Sets the end time of a trip
    Task<bool> UpdateEndTimestampAsync(Guid tripId, DateTime endTimestamp);

    // Returns stored trips page by page
    Task<(List<Trip> Trips, int TotalCount)> GetPageAsync(int page, int pageSize);

    // Deletes the trip with the given id
    Task<bool> DeleteAsync(Guid id);

    // Checks whether trips already exist for the given vehicle
    Task<bool> ExistsForVehicleAsync(string vehicleId);

    // Checks whether trips already exist for the given telemetry unit
    Task<bool> ExistsForTelemetryUnitAsync(Guid telemetryUnitId);

    // Checks whether trips already exist for the given driver
    Task<bool> ExistsForDriverAsync(Guid driverId);
}
