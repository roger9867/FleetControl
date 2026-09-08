using FleetControlServer.Data.Logging;
using FleetControlServer.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FleetControlServer.Data.Repos;

public class TripRepository : ITripRepository
{
    private readonly AppDbContext _context;
    private readonly ILogger<TripRepository> _logger;

    public TripRepository(AppDbContext context, ILogger<TripRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task CreateAsync(Trip entity)
    {
        _context.Trips.Add(entity);

        await _context.SaveChangesAsync();

        _logger.LogHighlighted(
            "Trip {tripId} inserted for TelemetryUnit {telemetryUnitId} (Start={start}, End={end})",
            entity.Id,
            entity.TelemetryUnitId,
            entity.StartTimestamp,
            entity.EndTimestamp);
    }

    public async Task<Trip?> GetOpenTripByTelemetryUnitIdAsync(Guid telemetryUnitId)
    {
        return await _context.Trips
            .Where(t => t.TelemetryUnitId == telemetryUnitId && t.EndTimestamp == null)
            .OrderByDescending(t => t.StartTimestamp)
            .FirstOrDefaultAsync();
    }

    public async Task<bool> UpdateEndTimestampAsync(Guid tripId, DateTime endTimestamp)
    {
        Trip? entity = await _context.Trips.FindAsync(tripId);

        if (entity == null)
            return false;

        entity.EndTimestamp = endTimestamp;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<Trip?> GetByIdAsync(Guid id)
    {
        return await _context.Trips.FindAsync(id);
    }

    public async Task<(List<Trip> Trips, int TotalCount)> GetPageAsync(int page, int pageSize)
    {
        // VehicleId/DriverId are snapshotted directly on Trip at creation time,
        // so no join through TelemetryUnit->Vehicle is needed to read them back.
        var query = _context.Trips
            .OrderByDescending(t => t.StartTimestamp);

        var totalCount = await query.CountAsync();

        var trips = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (trips, totalCount);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        Trip? entity = await _context.Trips.FindAsync(id);

        if (entity == null)
            return false;

        _context.Trips.Remove(entity);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ExistsForVehicleAsync(string vehicleId)
    {
        return await _context.Trips.AnyAsync(t => t.VehicleId == vehicleId);
    }

    public async Task<bool> ExistsForTelemetryUnitAsync(Guid telemetryUnitId)
    {
        return await _context.Trips.AnyAsync(t => t.TelemetryUnitId == telemetryUnitId);
    }

    public async Task<bool> ExistsForDriverAsync(Guid driverId)
    {
        return await _context.Trips.AnyAsync(t => t.DriverId == driverId);
    }
}
