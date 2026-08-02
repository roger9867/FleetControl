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
        return await _context.Vehicles
            .Include(v => v.TelemetryUnit)
            .ToListAsync();
    }

    public async Task<(bool Success, string? Error)> UpsertAsync(Vehicle vehicle)
    {
        try
        {
            var existing = await _context.Vehicles
                .FirstOrDefaultAsync(v => v.Id == vehicle.Id);

            if (existing == null)
            {
                _context.Vehicles.Add(vehicle);
            }
            else
            {
                _context.Entry(existing).CurrentValues.SetValues(vehicle);
            }

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
                        "IX_Vehicles_IdentificationNumber" =>
                            (false, "Identification number already exists."),

                        "IX_Vehicles_LicensePlateNumber" =>
                            (false, "License plate number already exists."),

                        "IX_Vehicles_FirstRegistration" =>
                            (false, "First registration date already exists."),

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
                .Include(v => v.TelemetryUnit)
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

    public async Task<bool> DriverExistsAsync(Guid id)
    {
        return await _context.VehicleDrivers.AnyAsync(d => d.Id == id);
    }

    public async Task<bool> SetDriverAsync(Guid vehicleId, Guid? driverId)
    {
        var vehicle = await _context.Vehicles.FindAsync(vehicleId);

        if (vehicle == null)
            return false;

        vehicle.VehicleDriverId = driverId;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task ClearDriverFromOtherVehiclesAsync(Guid driverId, Guid? exceptVehicleId)
    {
        var vehicles = await _context.Vehicles
            .Where(v => v.VehicleDriverId == driverId && v.Id != exceptVehicleId)
            .ToListAsync();

        if (vehicles.Count == 0)
            return;

        foreach (var vehicle in vehicles)
        {
            vehicle.VehicleDriverId = null;
        }

        await _context.SaveChangesAsync();
    }
}
