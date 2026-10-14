using FleetControlApi.Domain;
using Microsoft.EntityFrameworkCore;

namespace FleetControlApi.Persistence;

// EF Core context for accessing all entities of the application and model configuration
public class AppDbContext : DbContext
{
    // Connection string
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }
    
    public DbSet<TelemetryUnit> TelemetryUnits { get; set; }
    public DbSet<VehicleDriver> VehicleDrivers { get; set; }
    public DbSet<DriversLicense> DriversLicenses { get; set; }
    public DbSet<Vehicle> Vehicles { get; set; }
    public DbSet<Trip> Trips { get; set; }


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // VIN has exactly 17 characters
        modelBuilder.Entity<Vehicle>()
            .Property(v => v.VehicleIdentificationNumber)
            .HasColumnType("character(17)");

           modelBuilder.Entity<Vehicle>()
               .HasIndex(v => v.LicensePlateNumber)
               .IsUnique();

           modelBuilder.Entity<Vehicle>()
               .HasIndex(v => v.FirstRegistration)
               .IsUnique();

        // FK column lives on TelemetryUnit
        //  Deleting a vehicle may only detach the TelemetryUnit, never delete it
        modelBuilder.Entity<Vehicle>()
            .HasOne(v => v.TelemetryUnit)
            .WithOne(t => t.Vehicle)
            .HasForeignKey<TelemetryUnit>(t => t.VehicleId)
            .OnDelete(DeleteBehavior.SetNull);

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

        modelBuilder.Entity<Trip>()
            .Property(t => t.VehicleId)
            .HasColumnType("character(17)");

        // Driver's licenses belong to exactly one driver. If the driver is deleted, they are deleted as well.
        modelBuilder.Entity<VehicleDriver>()
            .HasMany(d => d.Licenses)
            .WithOne(l => l.VehicleDriver)
            .HasForeignKey(l => l.VehicleDriverId)
            .OnDelete(DeleteBehavior.Cascade);

        // Stored as the enum member name instead of as a numeric index
        modelBuilder.Entity<DriversLicense>()
            .Property(l => l.LicenseType)
            .HasConversion<string>();
    }
    
}
