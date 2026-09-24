using FleetControlServer.Service;
using FleetControlServer.Service.DTO.Vehicle;
using Microsoft.AspNetCore.Mvc;

namespace FleetControlServer.Api.Controllers;

// REST-Endpunkte zur Verwaltung von Fahrzeugen (Anlegen, Aktualisieren, Abfragen, Positionen, Löschen).
[ApiController]
[Route("api/[controller]")]
public class VehicleController : ControllerBase
{
    private readonly VehicleService _service;

    // Erstellt den Controller und injiziert den zugehörigen VehicleService.
    public VehicleController(VehicleService service)
    {
        _service = service;
    }


    // Liefert alle vorhandenen Fahrzeuge zurück.
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var vehicles = await _service.GetAllAsync();

        return Ok(vehicles);
    }


    // Liefert die zuletzt bekannten Positionen aller Fahrzeuge zurück.
    [HttpGet("positions")]
    public async Task<IActionResult> GetPositions()
    {
        var positions = await _service.GetLastPositionsAsync();

        return Ok(positions);
    }


    // Legt ein neues Fahrzeug anhand der übergebenen Daten an.
    [HttpPost]
    public async Task<IActionResult> Save([FromBody] VehicleDto dto)
    {
        var result = await _service.CreateAsync(dto);

        if (!result.Success)
        {
            return BadRequest(result.Error);
        }

        return Ok(result.Vehicle);
    }


    // Aktualisiert das Fahrzeug mit der angegebenen Id anhand der übergebenen Daten.
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, [FromBody] VehicleDto dto)
    {
        var result = await _service.UpsertAsync(id, dto);

        if (!result.Success)
        {
            return BadRequest(result.Error);
        }

        return Ok(result.Vehicle);
    }


    // Liefert das Fahrzeug mit der angegebenen Id oder 404, falls es nicht existiert.
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


    // Löscht das Fahrzeug mit der angegebenen Id.
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
