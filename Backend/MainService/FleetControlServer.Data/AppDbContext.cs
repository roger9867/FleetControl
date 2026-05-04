using FleetControlServer.Domain;
using Microsoft.EntityFrameworkCore;

namespace FleetControlServer.Data;

public class AppDbContext : DbContext
{
    // Connection string über Dependency Injection
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }
    
    public DbSet<VehicleTelemetryUnit> VehicleTelemetryUnits { get; set; }
    
    public DbSet<VehicleDriver> VehicleDrivers { get; set; }
    
}
