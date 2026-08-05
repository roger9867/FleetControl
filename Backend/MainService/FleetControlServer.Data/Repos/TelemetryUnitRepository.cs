using FleetControlServer.Domain;
using Microsoft.EntityFrameworkCore;

namespace FleetControlServer.Data.Repos;

public class TelemetryUnitRepository : ITelemetryUnitRepository
{
    private readonly AppDbContext _context;

    public TelemetryUnitRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task CreateAsync(TelemetryUnit entity)
    {
        _context.TelemetryUnits.Add(entity);

        await _context.SaveChangesAsync();
    }

    public async Task<List<TelemetryUnit>> GetAllAsync()
    {
        return await _context.TelemetryUnits.ToListAsync();
    }
    
    public async Task<bool> ExistsAsync(Guid id)
    {
        return await _context.TelemetryUnits
            .AnyAsync(x => x.Id == id);
    }

    public async Task<TelemetryUnit?> GetByIdAsync(Guid id)
    {
        return await _context.TelemetryUnits.FindAsync(id);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        TelemetryUnit entity = await _context.TelemetryUnits.FindAsync(id);

        if (entity == null)
            return false;

        _context.TelemetryUnits.Remove(entity);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> SetVehicleAsync(Guid telemetryUnitId, string? vehicleId)
    {
        TelemetryUnit entity = await _context.TelemetryUnits.FindAsync(telemetryUnitId);

        if (entity == null)
            return false;

        entity.VehicleId = vehicleId;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task ClearVehicleAsync(string vehicleId)
    {
        var units = await _context.TelemetryUnits
            .Where(u => u.VehicleId == vehicleId)
            .ToListAsync();

        if (units.Count == 0)
            return;

        foreach (var unit in units)
        {
            unit.VehicleId = null;
        }

        await _context.SaveChangesAsync();
    }
}
