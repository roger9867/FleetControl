using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace FleetControlServer.Domain;

public class DriversLicense
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public DriversLicenseType LicenseType { get; set; }

    [Required]
    public DateOnly ObtainedDate { get; set; }

    public Guid VehicleDriverId { get; set; }

    [JsonIgnore]
    public VehicleDriver? VehicleDriver { get; set; }

    public DriversLicense() {}

    public DriversLicense(DriversLicenseType type, DateOnly obtainedDate)
    {
        LicenseType = type;
        ObtainedDate = obtainedDate;
    }
}
