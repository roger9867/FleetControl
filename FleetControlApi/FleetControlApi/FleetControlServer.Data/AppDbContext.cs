using FleetControlServer.Domain;
using Microsoft.EntityFrameworkCore;

namespace FleetControlServer.Data;

// EF-Core-Kontext für den Zugriff auf alle Entitäten der Anwendung und Modellkonfiguration
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

        // FIN hat genau 17 Zeichen
        modelBuilder.Entity<Vehicle>()
            .Property(v => v.VehicleIdentificationNumber)
            .HasColumnType("character(17)");

           modelBuilder.Entity<Vehicle>()
               .HasIndex(v => v.LicensePlateNumber)
               .IsUnique();

           modelBuilder.Entity<Vehicle>()
               .HasIndex(v => v.FirstRegistration)
               .IsUnique();

        // FK-Spalte liegt auf TelemetryUnit
        //  Das Löschen eines Vehicle darf die TelemetryUnit nur entkoppeln, niemals löschen
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

        // Führerscheine gehören zu genau einem Fahrer. Wird der Fahrer gelöscht, werden sie mitgelöscht.
        modelBuilder.Entity<VehicleDriver>()
            .HasMany(d => d.Licenses)
            .WithOne(l => l.VehicleDriver)
            .HasForeignKey(l => l.VehicleDriverId)
            .OnDelete(DeleteBehavior.Cascade);

        // Wird als Name des Enum-Members gespeichert statt als numerischerIndex
        modelBuilder.Entity<DriversLicense>()
            .Property(l => l.LicenseType)
            .HasConversion<string>();
    }
    
}
