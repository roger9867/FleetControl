
using FleetControlApi.Service;
using FleetControlApi.Api.Dto;

namespace FleetControlApi.Api.Controllers;

using Microsoft.AspNetCore.Mvc;


// REST endpoints for managing telemetry units (create, update, query, delete).
[ApiController]
[Route("api/[controller]")]
public class TelemetryUnitController : ControllerBase
{
    private readonly TelemetryUnitService _service;

    // Creates the controller and injects the associated TelemetryUnitService.
    public TelemetryUnitController(TelemetryUnitService service)
    {
        _service = service;
    }

    // Creates a new telemetry unit from the given data.
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] TelemetryUnitDto dto)
    {
        bool result = await _service.CreateAsync(dto.ToDomain(dto.Id));

        if (!result)
        {
            return Conflict($"TelemetryUnit with id '{dto.Id}' could not be created.");
        }

        return Created($"/api/TelemetryUnit/{dto.Id}", new
        {
            id = dto.Id
        });
    }


    // Updates the telemetry unit with the given id using the given data.
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] TelemetryUnitDto dto)
    {
        bool success = await _service.UpdateAsync(dto.ToDomain(id));

        if (!success)
        {
            return NotFound($"TelemetryUnit with id '{id}' could not be updated.");
        }

        return Ok(new TelemetryUnitDto { Id = id, VehicleId = dto.VehicleId });
    }


    // Returns all existing telemetry units.
    [HttpGet("TelemetryUnits")]
    public async Task<IActionResult> GetAllVehicleTelemetryUnits()
    {
        var units = await _service.GetAllAsync();

        List<TelemetryUnitDto> responses = units.Select(TelemetryUnitDto.FromDomain).ToList();

        return Ok(responses);
    }


    // Deletes the telemetry unit with the given id.
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
