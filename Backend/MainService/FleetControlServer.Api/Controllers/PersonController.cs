using FleetControlServer.Service;
using FleetControlServer.Service.DTO.Person;
using Microsoft.AspNetCore.Mvc;

namespace FleetControlServer.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PersonController : ControllerBase
{
    private readonly PersonService _service;

    public PersonController(PersonService service)
    {
        _service = service;
    }


    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var persons = await _service.GetAllAsync();

        return Ok(persons);
    }


    [HttpPost]
    public async Task<IActionResult> Save([FromBody] PersonDto dto)
    {
        var result = await _service.UpsertAsync(Guid.NewGuid(), dto);

        if (!result.Success)
        {
            return BadRequest(result.Error);
        }

        return Ok(result.Person);
    }


    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] PersonDto dto)
    {
        var result = await _service.UpsertAsync(id, dto);

        if (!result.Success)
        {
            return BadRequest(result.Error);
        }

        return Ok(result.Person);
    }


    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _service.GetByIdAsync(id);

        if (!result.Success)
        {
            return NotFound(result.Error);
        }

        return Ok(result.Person);
    }


    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _service.DeleteAsync(id);

        return NoContent();
    }
}
