using FleetControlServer.Domain;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace FleetControlServer.Data.Repos;

public class VehicleRepository : IVehicleRepository
{
    
    private readonly AppDbContext _context;

    public VehicleRepository(AppDbContext context)
    {
        _context = context;
    }
    
    
    public async Task<IEnumerable<Vehicle>> GetAllAsync()
    {
        return await _context.Vehicles.ToListAsync();
    }

    public async Task<(bool Success, string? Error)> CreateAsync(Vehicle vehicle)
    {
        try
        {
            _context.Vehicles.Add(vehicle);

            await _context.SaveChangesAsync();

            return (true, null);
        }
        catch (DbUpdateException ex)
        {
            if (ex.InnerException is PostgresException pgEx)
            {
                // PostgreSQL unique violation
                if (pgEx.SqlState == "23505")
                {
                    return pgEx.ConstraintName switch
                    {
                        "UX_Vehicle_IdentificationNumber" =>
                            (false, "Identification number already exists."),

                        "UX_Vehicle_LicensePlateNumber" =>
                            (false, "License plate number already exists."),

                        _ => (false, "A unique constraint was violated.")
                    };
                }
            }

            throw;
        }
    }

    public async Task DeleteAsync(Vehicle vehicle)
    {
        try
        {
            vehicle = await _context.Vehicles
                .FirstOrDefaultAsync(v => v.Id == vehicle.Id);

            if (vehicle == null)
                return;

            _context.Vehicles.Remove(vehicle);

            await _context.SaveChangesAsync();
        }
        catch
        {
            // intentionally ignored
        }
    }

    public async Task<(bool Success, Vehicle? Vehicle, string? Error)> GetByIdAsync(Guid id)
    {
        try
        {
            var vehicle = await _context.Vehicles
                .FirstOrDefaultAsync(v => v.Id == id);

            if (vehicle == null)
            {
                return (false, null, "Vehicle not found.");
            }

            return (true, vehicle, null);
        }
        catch
        {
            return (false, null, "Failed to retrieve vehicle.");
        }
    }
}
