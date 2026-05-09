using System.Collections;
using FleetControlServer.Data.Repos;
using FleetControlServer.Domain;
using FleetControlServer.Service.DTO.Vehicle;

namespace FleetControlServer.Service;
public class VehicleService
{
    private readonly IVehicleRepository _repo;

    public VehicleService(IVehicleRepository repo)
    {
        _repo = repo;
    }
    
    public async Task<(bool Success, string? Error)> CreateAsync(VehicleDto dto)
    {
        var entity = new Vehicle
        {
            IdentificationNumber = dto.IdentificationNumber,
            LicensePlateNumber = dto.LicensePlateNumber,
            ModelName =  dto.ModelName,
        };

        return await _repo.CreateAsync(entity);
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
