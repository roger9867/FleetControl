using FleetControlServer.Domain;

namespace FleetControlServer.Data.Repos;

public interface ITripRepository
{
    Task CreateAsync(Trip entity);

    Task<Trip?> GetOpenTripByTelemetryUnitIdAsync(Guid telemetryUnitId);

    Task<bool> UpdateEndTimestampAsync(Guid tripId, DateTime endTimestamp);
}
