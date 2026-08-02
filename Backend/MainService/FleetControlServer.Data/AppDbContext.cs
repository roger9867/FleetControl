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
    public DbSet<Trip> Trips { get; set; }


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Vehicle>()
               .HasIndex(v => v.IdentificationNumber)
               .IsUnique();

           modelBuilder.Entity<Vehicle>()
               .HasIndex(v => v.LicensePlateNumber)
               .IsUnique();

           modelBuilder.Entity<Vehicle>()
               .HasIndex(v => v.FirstRegistration)
               .IsUnique();

        // Vehicle is the principal; the FK column lives on TelemetryUnit. Deleting a
        // Vehicle must only unlink its TelemetryUnit, never delete it.
        modelBuilder.Entity<Vehicle>()
            .HasOne(v => v.TelemetryUnit)
            .WithOne(t => t.Vehicle)
            .HasForeignKey<TelemetryUnit>(t => t.VehicleId)
            .OnDelete(DeleteBehavior.SetNull);

        // Vehicle is the dependent here, so deleting a Vehicle never touches the
        // VehicleDriver row; SetNull only matters if the driver itself gets deleted.
        modelBuilder.Entity<Vehicle>()
            .HasOne(v => v.VehicleDriver)
            .WithMany()
            .HasForeignKey(v => v.VehicleDriverId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<Trip>()
            .HasOne(t => t.TelemetryUnit)
            .WithMany()
            .HasForeignKey(t => t.TelemetryUnitId)
            .OnDelete(DeleteBehavior.Restrict);
    }
    
}
