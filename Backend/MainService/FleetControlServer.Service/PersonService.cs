using FleetControlServer.Data.Repos;
using FleetControlServer.Domain;
using FleetControlServer.Service.DTO.Person;

namespace FleetControlServer.Service;

public class PersonService
{
    private readonly IVehicleDriverRepository _repo;
    private readonly IVehicleRepository _vehicleRepo;

    public PersonService(IVehicleDriverRepository repo, IVehicleRepository vehicleRepo)
    {
        _repo = repo;
        _vehicleRepo = vehicleRepo;
    }

    public async Task<(bool Success, VehicleDriver? Person, string? Error)> UpsertAsync(Guid id, PersonDto dto)
    {
        var licenses = new List<DriversLicense>();
        foreach (var licenseDto in dto.Licenses ?? new List<DrivingLicenseDto>())
        {
            if (!Enum.TryParse(licenseDto.LicenseClass, out DriversLicenseType parsedType))
            {
                return (false, null, $"Unknown drivers license type: {licenseDto.LicenseClass}");
            }

            licenses.Add(new DriversLicense
            {
                VehicleDriverId = id,
                LicenseType = parsedType,
                ObtainedDate = licenseDto.ObtainedDate
            });
        }

        // Vehicle assignment is optional, but if given it must exist.
        if (dto.AssignedVehicleId.HasValue)
        {
            var vehicleResult = await _vehicleRepo.GetByIdAsync(dto.AssignedVehicleId.Value);

            if (!vehicleResult.Success)
            {
                return (false, null, "Assigned vehicle not found.");
            }
        }

        var entity = new VehicleDriver
        {
            Id = id,
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            DateOfBirth = dto.BirthDate,
            Licenses = licenses
        };

        var (success, error) = await _repo.UpsertAsync(entity);

        if (!success)
        {
            return (false, null, error);
        }

        // Keep the Vehicle <-> Driver assignment in sync from this side too:
        // unlink any vehicle that used to point at this driver but shouldn't anymore,
        // then link the newly assigned one (if any).
        await _vehicleRepo.ClearDriverFromOtherVehiclesAsync(entity.Id, dto.AssignedVehicleId);

        if (dto.AssignedVehicleId.HasValue)
        {
            await _vehicleRepo.SetDriverAsync(dto.AssignedVehicleId.Value, entity.Id);
        }

        return (true, entity, null);
    }

    public async Task<IEnumerable<VehicleDriver>> GetAllAsync()
    {
        return await _repo.GetAllAsync();
    }

    public async Task<(bool Success, VehicleDriver? Person, string? Error)> GetByIdAsync(Guid id)
    {
        return await _repo.GetByIdAsync(id);
    }

    public async Task DeleteAsync(Guid id)
    {
        await _vehicleRepo.ClearDriverFromOtherVehiclesAsync(id, null);
        await _repo.DeleteAsync(id);
    }
}
