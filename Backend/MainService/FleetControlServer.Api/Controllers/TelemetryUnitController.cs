
using FleetControlServer.Domain;
using FleetControlServer.Service;
using FleetControlServer.Service.Dto.TelemetryUnit;

namespace FleetControlServer.Api.Controllers;

using Microsoft.AspNetCore.Mvc;


[ApiController]
[Route("api/[controller]")]
public class TelemetryUnitController : ControllerBase
{
    // Controller gets service with ASP.NET dependeny injection through constructor
    private readonly TelemetryUnitService _service;

    public TelemetryUnitController(TelemetryUnitService service)
    {
        _service = service;
    }
    
    [HttpPost("broadcast")]
    public async Task<IActionResult> Broadcast([FromBody] string commandMessage)
    {
        var responses = await _service.BroadcastCommandAsync(commandMessage);
        return Ok(responses);
    }
    
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] TelemetryUnitDto dto)
    {
        bool result = await _service.CreateAsync(dto);

        if (!result)
        {
            return Conflict($"TelemetryUnit with id '{dto.Id}' could not be created.");
        }

        return Created($"/api/TelemetryUnit/{dto.Id}", new
        {
            id = dto.Id
        });
    }

    
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] TelemetryUnitDto dto)
    {
        bool success = await _service.UpdateAsync(id, dto);

        if (!success)
        {
            return NotFound($"TelemetryUnit with id '{id}' could not be updated.");
        }

        return Ok(new TelemetryUnitDto { Id = id, VehicleId = dto.VehicleId });
    }


    [HttpGet("TelemetryUnits")]
    public async Task<IActionResult> GetAllVehicleTelemetryUnits()
    {
        List<TelemetryUnitDto> responses = await _service.GetAllAsync();
        
        return Ok(responses);
    }


    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {

        bool couldBeDeleted  = await _service.DeleteAsync(id);
        
        if (!couldBeDeleted)
            return NotFound($"VehicleTelemetryUnit with id '{id}' could not be deleted.");
        
        return NoContent();
    }
}
