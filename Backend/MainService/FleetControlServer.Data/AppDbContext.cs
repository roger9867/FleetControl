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
    
    public DbSet<TelemetryUnit> TelemetryUnits { get; set; }
    public DbSet<VehicleDriver> VehicleDrivers { get; set; }
    public DbSet<Vehicle> Vehicles { get; set; }
    
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        /*
        modelBuilder.Entity<Vehicle>()
            //.HasOne(v => v.LicenseNeededToDrive)
            ((.WithMany()
            .HasForeignKey(v => v.LicenseNeededToDriveId)
            .IsRequired(false);
            */
    }
    
}
