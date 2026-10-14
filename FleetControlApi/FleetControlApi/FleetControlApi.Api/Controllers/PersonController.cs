using FleetControlApi.Api.Dto;
using FleetControlApi.Service;
using Microsoft.AspNetCore.Mvc;

namespace FleetControlApi.Api.Controllers;

// REST endpoints for managing persons (create, update, query, delete).
[ApiController]
[Route("api/[controller]")]
public class PersonController : ControllerBase
{
    private readonly IPersonService _service;

    // Creates the controller and injects the associated PersonService.
    public PersonController(IPersonService service)
    {
        _service = service;
    }


    // Returns all existing persons.
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var persons = await _service.GetAllAsync();

        return Ok(persons);
    }


    // Creates a new person with a generated id from the given data.
    [HttpPost]
    public async Task<IActionResult> Save([FromBody] PersonDto dto)
    {
        if (!dto.TryToDomain(Guid.NewGuid(), out var person, out var error))
        {
            return BadRequest(error);
        }

        var result = await _service.UpsertAsync(person, dto.AssignedVehicleId);

        if (!result.Success)
        {
            return BadRequest(result.Error);
        }

        return Ok(result.Person);
    }


    // Updates the person with the given id using the given data.
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] PersonDto dto)
    {
        if (!dto.TryToDomain(id, out var person, out var error))
        {
            return BadRequest(error);
        }

        var result = await _service.UpsertAsync(person, dto.AssignedVehicleId);

        if (!result.Success)
        {
            return BadRequest(result.Error);
        }

        return Ok(result.Person);
    }


    // Returns the person with the given id, or 404 if it does not exist.
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


    // Deletes the person with the given id.
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var (success, error) = await _service.DeleteAsync(id);

        if (!success)
        {
            return BadRequest(error);
        }

        return NoContent();
    }
}
