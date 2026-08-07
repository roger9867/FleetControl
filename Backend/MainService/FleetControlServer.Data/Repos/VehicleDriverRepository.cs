using FleetControlServer.Domain;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace FleetControlServer.Data.Repos;

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

                // Licenses are a child collection — CurrentValues.SetValues only
                // touches scalar properties, so replace them explicitly. Each
                // DriversLicense already has a client-generated Id (see
                // DriversLicense.Id's default), so simply reassigning the
                // navigation property makes EF's graph-tracking assume these
                // rows already exist (Unchanged) instead of Added, which then
                // throws a DbUpdateConcurrencyException ("0 rows affected") on
                // save. Adding them explicitly forces the correct Added state.
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
                return (false, "A unique constraint was violated.");
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
            // intentionally ignored
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
                return (false, null, "Person not found.");
            }

            return (true, driver, null);
        }
        catch
        {
            return (false, null, "Failed to retrieve person.");
        }
    }
}
