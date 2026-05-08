using FleetControlServer.Service;
using FleetControlServer.Service.DTO.Vehicle;
using Microsoft.AspNetCore.Mvc;

namespace FleetControlServer.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class VehicleController : ControllerBase
{
    private readonly VehicleService _service;

    public VehicleController(VehicleService service)
    {
        _service = service;
    }
    
    
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] VehicleDto dto)
    {
        var result = await _service.CreateAsync(dto);

        if (!result.Success)
        {
            return BadRequest(result.Error);
        }

        return Ok();
    }
    
    
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _service.GetByIdAsync(id);

        if (!result.Success)
        {
            return NotFound(result.Error);
        }

        return Ok(result.Vehicle);
    }
    
    
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _service.DeleteAsync(id);

        return NoContent();
    }
}
