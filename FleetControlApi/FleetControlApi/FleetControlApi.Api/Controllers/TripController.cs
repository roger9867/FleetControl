using FleetControlApi.Api.Dto;
using FleetControlApi.Service;
using Microsoft.AspNetCore.Mvc;

namespace FleetControlApi.Api.Controllers;

// REST endpoints for managing trips (paged queries, delete).
[ApiController]
[Route("api/[controller]")]
public class TripController : ControllerBase
{
    private readonly TripService _service;

    // Creates the controller and injects the associated TripService.
    public TripController(TripService service)
    {
        _service = service;
    }

    // Returns a page of trips including their InfluxDB data points loaded live via gRPC.
    // Always caps at 10 trips per page (enforced in TripService), each carrying
    // its InfluxDB points fetched live from TelemetryDataService via gRPC.
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var (trips, totalCount) = await _service.GetPageAsync(page, pageSize);

        return Ok(new TripPageDto
        {
            Trips = trips.Select(TripDto.FromDomain).ToList(),
            TotalCount = totalCount
        });
    }

    // Deletes the trip with the given id.
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
