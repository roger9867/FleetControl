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

        modelBuilder.Entity<Trip>()
            .HasOne(t => t.TelemetryUnit)
            .WithMany()
            .HasForeignKey(t => t.TelemetryUnitId)
            .OnDelete(DeleteBehavior.Restrict);
    }
    
}
