using FleetControlApi.Service;
using FleetControlApi.Api.Dto;
using Microsoft.AspNetCore.Mvc;

namespace FleetControlApi.Api.Controllers;

// REST endpoints for managing vehicles (create, update, query, positions, delete).
[ApiController]
[Route("api/[controller]")]
public class VehicleController : ControllerBase
{
    private readonly VehicleService _service;

    // Creates the controller and injects the associated VehicleService.
    public VehicleController(VehicleService service)
    {
        _service = service;
    }


    // Returns all existing vehicles.
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var vehicles = await _service.GetAllAsync();

        return Ok(vehicles);
    }


    // Returns the last known positions of all vehicles.
    [HttpGet("positions")]
    public async Task<IActionResult> GetPositions()
    {
        var positions = await _service.GetLastPositionsAsync();

        return Ok(positions
            .Select(p => VehiclePositionDto.FromDomain(p.Key, p.Value))
            .ToList());
    }


    // Creates a new vehicle from the given data.
    [HttpPost]
    public async Task<IActionResult> Save([FromBody] VehicleDto dto)
    {
        var result = await _service.CreateAsync(dto.ToDomain(), dto.TelemetryUnitId);

        if (!result.Success)
        {
            return BadRequest(result.Error);
        }

        return Ok(result.Vehicle);
    }


    // Updates the vehicle with the given id using the given data.
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, [FromBody] VehicleDto dto)
    {
        var result = await _service.UpsertAsync(id, dto.ToDomain(), dto.TelemetryUnitId);

        if (!result.Success)
        {
            return BadRequest(result.Error);
        }

        return Ok(result.Vehicle);
    }


    // Returns the vehicle with the given id, or 404 if it does not exist.
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id)
    {
        var result = await _service.GetByIdAsync(id);

        if (!result.Success)
        {
            return NotFound(result.Error);
        }

        return Ok(result.Vehicle);
    }


    // Deletes the vehicle with the given id.
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        var (success, error) = await _service.DeleteAsync(id);

        if (!success)
        {
            return BadRequest(error);
        }

        return NoContent();
    }
}
