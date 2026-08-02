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


    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var vehicles = await _service.GetAllAsync();

        return Ok(vehicles);
    }


    [HttpPost]
    public async Task<IActionResult> Save([FromBody] VehicleDto dto)
    {
        var result = await _service.UpsertAsync(Guid.NewGuid(), dto);

        if (!result.Success)
        {
            return BadRequest(result.Error);
        }

        return Ok(result.Vehicle);
    }


    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] VehicleDto dto)
    {
        var result = await _service.UpsertAsync(id, dto);

        if (!result.Success)
        {
            return BadRequest(result.Error);
        }

        return Ok(result.Vehicle);
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
