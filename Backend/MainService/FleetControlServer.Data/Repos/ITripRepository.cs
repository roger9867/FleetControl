using FleetControlServer.Domain;

namespace FleetControlServer.Data.Repos;

public interface ITripRepository
{
    Task CreateAsync(Trip entity);

    Task<Trip?> GetOpenTripByTelemetryUnitIdAsync(Guid telemetryUnitId);

    Task<bool> UpdateEndTimestampAsync(Guid tripId, DateTime endTimestamp);

    // Most recent trips first, with TelemetryUnit->Vehicle eager-loaded so the
    // caller can resolve VehicleId/DriverId without a second round trip.
    Task<(List<Trip> Trips, int TotalCount)> GetPageAsync(int page, int pageSize);
}
