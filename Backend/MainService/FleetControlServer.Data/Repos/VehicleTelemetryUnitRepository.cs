using FleetControlServer.Domain;
using Microsoft.EntityFrameworkCore;

namespace FleetControlServer.Data.Repos;

public class VehicleTelemetryUnitRepository : IVehicleTelemetryUnitRepository
{
    private readonly AppDbContext _context;

    public VehicleTelemetryUnitRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task CreateAsync(VehicleTelemetryUnit entity)
    {
        _context.VehicleTelemetryUnits.Add(entity);

        await _context.SaveChangesAsync();
    }

    public async Task<List<VehicleTelemetryUnit>> GetAllAsync()
    {
        return await _context.VehicleTelemetryUnits.ToListAsync();
    }
    
    public async Task<bool> ExistsAsync(Guid id)
    {
        return await _context.VehicleTelemetryUnits
            .AnyAsync(x => x.Id == id);
    }
}
