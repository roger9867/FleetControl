using FleetControlApi.Domain;
using Microsoft.EntityFrameworkCore;
using Npgsql;

using FleetControlApi.Service.Repositories;

namespace FleetControlApi.Persistence.Repos;

// EF Core implementation of data access for vehicles.
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
                .FirstOrDefaultAsync(v => v.VehicleIdentificationNumber == vehicle.VehicleIdentificationNumber);

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
                        "PK_Vehicles" =>
                            (false, "Ein Fahrzeug mit dieser Identifikationsnummer existiert bereits."),

                        "IX_Vehicles_LicensePlateNumber" =>
                            (false, "Dieses Kennzeichen ist bereits vergeben."),

                        "IX_Vehicles_FirstRegistration" =>
                            (false, "Dieses Erstzulassungsdatum ist bereits vergeben."),

                        _ => (false, "Eine Eindeutigkeitsbedingung wurde verletzt.")
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
                .FirstOrDefaultAsync(v => v.VehicleIdentificationNumber == vehicle.VehicleIdentificationNumber);

            if (vehicle == null)
                return;

            _context.Vehicles.Remove(vehicle);

            await _context.SaveChangesAsync();
        }
        catch
        {
        }
    }

    public async Task<(bool Success, Vehicle? Vehicle, string? Error)> GetByIdAsync(string id)
    {
        try
        {
            var vehicle = await _context.Vehicles
                .Include(v => v.TelemetryUnit)
                .FirstOrDefaultAsync(v => v.VehicleIdentificationNumber == id);

            if (vehicle == null)
            {
                return (false, null, "Fahrzeug nicht gefunden.");
            }

            return (true, vehicle, null);
        }
        catch
        {
            return (false, null, "Fahrzeug konnte nicht abgerufen werden.");
        }
    }

    public async Task<bool> DriverExistsAsync(Guid id)
    {
        return await _context.VehicleDrivers.AnyAsync(d => d.Id == id);
    }

    public async Task<bool> SetDriverAsync(string vehicleId, Guid? driverId)
    {
        var vehicle = await _context.Vehicles.FindAsync(vehicleId);

        if (vehicle == null)
            return false;

        vehicle.VehicleDriverId = driverId;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task ClearDriverFromOtherVehiclesAsync(Guid driverId, string? exceptVehicleId)
    {
        var vehicles = await _context.Vehicles
            .Where(v => v.VehicleDriverId == driverId && v.VehicleIdentificationNumber != exceptVehicleId)
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
