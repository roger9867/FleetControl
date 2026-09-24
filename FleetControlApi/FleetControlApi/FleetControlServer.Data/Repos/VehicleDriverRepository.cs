using FleetControlServer.Domain;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace FleetControlServer.Data.Repos;

// EF-Core-Implementierung des Datenzugriffs für Fahrer.
public class VehicleDriverRepository : IVehicleDriverRepository
{
    private readonly AppDbContext _context;

    public VehicleDriverRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<VehicleDriver>> GetAllAsync()
    {
        return await _context.VehicleDrivers
            .Include(d => d.Licenses)
            .ToListAsync();
    }

    public async Task<(bool Success, string? Error)> UpsertAsync(VehicleDriver driver)
    {
        try
        {
            var existing = await _context.VehicleDrivers
                .Include(d => d.Licenses)
                .FirstOrDefaultAsync(d => d.Id == driver.Id);

            if (existing == null)
            {
                _context.VehicleDrivers.Add(driver);
            }
            else
            {
                _context.Entry(existing).CurrentValues.SetValues(driver);
                _context.DriversLicenses.RemoveRange(existing.Licenses);
                existing.Licenses.Clear();
                _context.DriversLicenses.AddRange(driver.Licenses);
            }

            await _context.SaveChangesAsync();

            return (true, null);
        }
        catch (DbUpdateException ex)
        {
            if (ex.InnerException is PostgresException pgEx && pgEx.SqlState == "23505")
            {
                return (false, "Eine Eindeutigkeitsbedingung wurde verletzt.");
            }

            throw;
        }
    }

    public async Task DeleteAsync(Guid id)
    {
        try
        {
            var driver = await _context.VehicleDrivers
                .FirstOrDefaultAsync(d => d.Id == id);

            if (driver == null)
                return;

            _context.VehicleDrivers.Remove(driver);

            await _context.SaveChangesAsync();
        }
        catch
        {
        }
    }

    public async Task<(bool Success, VehicleDriver? Person, string? Error)> GetByIdAsync(Guid id)
    {
        try
        {
            var driver = await _context.VehicleDrivers
                .Include(d => d.Licenses)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (driver == null)
            {
                return (false, null, "Person nicht gefunden.");
            }

            return (true, driver, null);
        }
        catch
        {
            return (false, null, "Person konnte nicht abgerufen werden.");
        }
    }
}
