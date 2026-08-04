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

    public async Task<(bool Success, Vehicle? Vehicle, string? Error)> CreateAsync(VehicleDto dto)
    {
        var id = dto.IdentificationNumber?.Trim();

        if (string.IsNullOrEmpty(id))
        {
            return (false, null, "Identification number is required.");
        }

        if (id.Length > 40)
        {
            return (false, null, "Identification number must be at most 40 characters.");
        }

        var existing = await _repo.GetByIdAsync(id);
        if (existing.Success)
        {
            return (false, null, "A vehicle with this identification number already exists.");
        }

        return await UpsertAsync(id, dto);
    }

    public async Task<(bool Success, Vehicle? Vehicle, string? Error)> UpsertAsync(string id, VehicleDto dto)
    {
        // The Id doubles as the identification number, so the two must never drift apart.
        if (!string.Equals(id, dto.IdentificationNumber?.Trim(), StringComparison.Ordinal))
        {
            return (false, null, "Identification number cannot be changed.");
        }

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
            IdentificationNumber = id,
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
        else
        {
            // "nicht verbunden" was chosen — release whatever unit was
            // previously assigned to this vehicle instead of leaving it linked.
            await _telemetryUnitRepo.ClearVehicleAsync(entity.Id);
        }

        // entity's TelemetryUnit navigation property was never populated above —
        // the FK update happened on the TelemetryUnit row, not on this instance.
        // Re-fetch so the response reflects the assignment that was just persisted.
        var (found, refreshed, fetchError) = await _repo.GetByIdAsync(entity.Id);

        if (!found)
        {
            return (false, null, fetchError);
        }

        return (true, refreshed, null);
    }

    public async Task<IEnumerable<Vehicle>> GetAllAsync()
    {
        return await _repo.GetAllAsync();
    }

    public async Task<(bool Success, Vehicle? Vehicle, string? Error)> GetByIdAsync(string id)
    {
        return await _repo.GetByIdAsync(id);
    }

    public async Task DeleteAsync(string id)
    {
        Vehicle vehicle = new Vehicle()
        {
            Id = id,
        };
        await _repo.DeleteAsync(vehicle);
    }
}
