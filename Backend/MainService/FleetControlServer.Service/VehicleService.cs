using FleetControlServer.Data.Repos;
using FleetControlServer.Domain;
using FleetControlServer.Service.DTO.Vehicle;

namespace FleetControlServer.Service;
public class VehicleService
{
    private readonly IVehicleRepository _repo;
    private readonly ITelemetryUnitRepository _telemetryUnitRepo;

    public VehicleService(IVehicleRepository repo, ITelemetryUnitRepository telemetryUnitRepo)
    {
        _repo = repo;
        _telemetryUnitRepo = telemetryUnitRepo;
    }

    public async Task<(bool Success, Vehicle? Vehicle, string? Error)> UpsertAsync(Guid id, VehicleDto dto)
    {
        DriversLicenseType? requiredLicense = null;
        if (!string.IsNullOrWhiteSpace(dto.RequiredLicense))
        {
            if (!Enum.TryParse(dto.RequiredLicense, out DriversLicenseType parsedLicense))
            {
                return (false, null, "Unknown drivers license type.");
            }

            requiredLicense = parsedLicense;
        }

        // Driver and telemetry unit are optional, but if an id was given it must exist.
        if (dto.VehicleDriverId.HasValue && !await _repo.DriverExistsAsync(dto.VehicleDriverId.Value))
        {
            return (false, null, "Driver not found.");
        }

        if (dto.TelemetryUnitId.HasValue && !await _telemetryUnitRepo.ExistsAsync(dto.TelemetryUnitId.Value))
        {
            return (false, null, "Telemetry unit not found.");
        }

        var entity = new Vehicle
        {
            Id = id,
            IdentificationNumber = dto.IdentificationNumber,
            LicensePlateNumber = dto.LicensePlateNumber,
            ModelName = dto.ModelName,
            Brand = dto.Brand,
            Year = dto.Year,
            RequiredLicense = requiredLicense,
            PowerPs = dto.PowerPs,
            Color = dto.Color,
            FirstRegistration = dto.FirstRegistration,
            VehicleDriverId = dto.VehicleDriverId,
        };

        var (success, error) = await _repo.UpsertAsync(entity);

        if (!success)
        {
            return (false, null, error);
        }

        if (dto.TelemetryUnitId.HasValue)
        {
            await _telemetryUnitRepo.SetVehicleAsync(dto.TelemetryUnitId.Value, entity.Id);
        }

        return (true, entity, null);
    }

    public async Task<IEnumerable<Vehicle>> GetAllAsync()
    {
        return await _repo.GetAllAsync();
    }

    public async Task<(bool Success, Vehicle? Vehicle, string? Error)> GetByIdAsync(Guid id)
    {
        return await _repo.GetByIdAsync(id);
    }

    public async Task DeleteAsync(Guid id)
    {
        Vehicle vehicle = new Vehicle()
        {
            Id = id,
        };
        await _repo.DeleteAsync(vehicle);
    }
}
