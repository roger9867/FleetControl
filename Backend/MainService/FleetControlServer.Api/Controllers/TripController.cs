using FleetControlServer.Service;
using Microsoft.AspNetCore.Mvc;

namespace FleetControlServer.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TripController : ControllerBase
{
    private readonly TripService _service;

    public TripController(TripService service)
    {
        _service = service;
    }

    // Always caps at 10 trips per page (enforced in TripService), each carrying
    // its InfluxDB points fetched live from TelemetryDataService via gRPC.
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var result = await _service.GetPageAsync(page, pageSize);

        return Ok(result);
    }

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
