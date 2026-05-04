using FleetControlServer.Service;

namespace FleetControlServer.Api.Controller;

using Microsoft.AspNetCore.Mvc;


[ApiController]
[Route("api/[controller]")]
public class VehicleTelemetryUnitController : ControllerBase
{
    
    private readonly VehicleTelemetryUnitService _service;

    public VehicleTelemetryUnitController(VehicleTelemetryUnitService service)
    {
        _service = service;
    }
    
    [HttpPost("broadcast")]
    public async Task<IActionResult> Broadcast([FromBody] string commandMessage)
    {
        var responses = await _service.BroadcastCommandAsync(commandMessage);
        return Ok(responses);
    }
    
    [HttpPost("{id}")]
    public async Task<IActionResult> Create(string id)
    {
        var result = await _service.CreateAsync(id);

        if (!result)
        {
            return Conflict($"VehicleTelemetryUnit with id '{id}' could not be created.");
        }

        return Created($"/api/VehicleTelemetryUnit/{id}", new
        {
            id = id
        });
    }

    /*
    [HttpGet("VehicleTelemetryUnits")]
    public async Task<IActionResult> GetAllVehicleTelemetryUnits()
    {
        var responses = await _service.GetAllVehicleTelemetryUnitsAsync();
        return Ok(responses);
    }*/
}
