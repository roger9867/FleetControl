using FleetControlServer.Service;
using FleetControlServer.Service.DTO.Person;
using Microsoft.AspNetCore.Mvc;

namespace FleetControlServer.Api.Controllers;

// REST-Endpunkte zur Verwaltung von Personen (Anlegen, Aktualisieren, Abfragen, Löschen).
[ApiController]
[Route("api/[controller]")]
public class PersonController : ControllerBase
{
    private readonly PersonService _service;

    // Erstellt den Controller und injiziert den zugehörigen PersonService.
    public PersonController(PersonService service)
    {
        _service = service;
    }


    // Liefert alle vorhandenen Personen zurück.
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var persons = await _service.GetAllAsync();

        return Ok(persons);
    }


    // Legt eine neue Person mit einer generierten Id anhand der übergebenen Daten an.
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


    // Aktualisiert die Person mit der angegebenen Id anhand der übergebenen Daten.
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


    // Liefert die Person mit der angegebenen Id oder 404, falls sie nicht existiert.
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


    // Löscht die Person mit der angegebenen Id.
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
