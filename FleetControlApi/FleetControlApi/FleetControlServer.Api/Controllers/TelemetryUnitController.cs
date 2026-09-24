
using FleetControlServer.Domain;
using FleetControlServer.Service;
using FleetControlServer.Service.Dto.TelemetryUnit;

namespace FleetControlServer.Api.Controllers;

using Microsoft.AspNetCore.Mvc;


// REST-Endpunkte zur Verwaltung von Telemetrie-Einheiten (Anlegen, Aktualisieren, Abfragen, Löschen).
[ApiController]
[Route("api/[controller]")]
public class TelemetryUnitController : ControllerBase
{
    private readonly TelemetryUnitService _service;

    // Erstellt den Controller und injiziert den zugehörigen TelemetryUnitService.
    public TelemetryUnitController(TelemetryUnitService service)
    {
        _service = service;
    }

    // Legt eine neue Telemetrie-Einheit anhand der übergebenen Daten an.
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


    // Aktualisiert die Telemetrie-Einheit mit der angegebenen Id anhand der übergebenen Daten.
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


    // Liefert alle vorhandenen Telemetrie-Einheiten zurück.
    [HttpGet("TelemetryUnits")]
    public async Task<IActionResult> GetAllVehicleTelemetryUnits()
    {
        List<TelemetryUnitDto> responses = await _service.GetAllAsync();

        return Ok(responses);
    }


    // Löscht die Telemetrie-Einheit mit der angegebenen Id.
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
