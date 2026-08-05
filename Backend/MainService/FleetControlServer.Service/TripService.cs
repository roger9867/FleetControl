using FleetControlServer.Domain;
using FleetControlServer.Data.Repos;
using FleetControlServer.Service.DTO.Trip;
using FleetControlServer.Service.Telemetry;

namespace FleetControlServer.Service;

public class TripService
{
    private const int MaxPageSize = 10;

    private readonly ITripRepository _tripRepository;
    private readonly ITelemetryUnitRepository _telemetryUnitRepository;
    private readonly IVehicleRepository _vehicleRepository;
    private readonly TelemetryQueryClient _telemetryQueryClient;

    public TripService(
        ITripRepository tripRepository,
        ITelemetryUnitRepository telemetryUnitRepository,
        IVehicleRepository vehicleRepository,
        TelemetryQueryClient telemetryQueryClient
        )
    {
        _tripRepository = tripRepository;
        _telemetryUnitRepository = telemetryUnitRepository;
        _vehicleRepository = vehicleRepository;
        _telemetryQueryClient = telemetryQueryClient;
    }

    // Always loads at most one page of MaxPageSize trips, then fetches that
    // page's InfluxDB points from TelemetryDataService via gRPC (one call per
    // trip, in parallel) so the map/list never has to pull more than 10 trips'
    // worth of telemetry at a time.
    public async Task<TripPageDto> GetPageAsync(int page, int pageSize)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, MaxPageSize);

        var (trips, totalCount) = await _tripRepository.GetPageAsync(page, pageSize);

        var tripDtos = await Task.WhenAll(trips.Select(BuildTripDtoAsync));

        return new TripPageDto
        {
            Trips = tripDtos.ToList(),
            TotalCount = totalCount
        };
    }

    private async Task<TripDto> BuildTripDtoAsync(Trip trip)
    {
        var start = DateTime.SpecifyKind(trip.StartTimestamp, DateTimeKind.Utc);
        var end = DateTime.SpecifyKind(trip.EndTimestamp ?? DateTime.UtcNow, DateTimeKind.Utc);

        var points = await _telemetryQueryClient.GetTelemetryPointsAsync(
            trip.TelemetryUnitId.ToString(),
            start,
            end);

        // VehicleId/DriverId are read straight from the trip row — they were
        // snapshotted once at trip start, not the TelemetryUnit's current
        // assignment, so a later reassignment can't rewrite this trip's history.
        return new TripDto
        {
            Id = trip.Id,
            VehicleId = trip.VehicleId,
            TelemetryUnitId = trip.TelemetryUnitId,
            DriverId = trip.DriverId,
            Start = trip.StartTimestamp,
            End = trip.EndTimestamp,
            Points = points.Select(p => new TripPointDto
            {
                Timestamp = p.Timestamp,
                Lat = p.Lat,
                Lng = p.Lon,
                SpeedKmh = p.SpeedKmh,
                AccelMs2 = p.AccelMs2
            }).ToList()
        };
    }

    // Resolves the Vehicle (and that Vehicle's driver) currently linked to a
    // TelemetryUnit — called only at trip creation, so the result gets
    // snapshotted onto the Trip row rather than re-resolved on every read.
    private async Task<(string? VehicleId, Guid? DriverId)> ResolveVehicleAndDriverAsync(TelemetryUnit telemetryUnit)
    {
        if (string.IsNullOrEmpty(telemetryUnit.VehicleId))
        {
            return (null, null);
        }

        var vehicleResult = await _vehicleRepository.GetByIdAsync(telemetryUnit.VehicleId);

        if (!vehicleResult.Success)
        {
            return (telemetryUnit.VehicleId, null);
        }

        return (telemetryUnit.VehicleId, vehicleResult.Vehicle!.VehicleDriverId);
    }

    // Legt einen neuen Trip an, sofern die referenzierte TelemetryUnit existiert.
    // Gibt null zurück, wenn die TelemetryUnit nicht existiert.
    public async Task<Trip?> StartTripAsync(Guid telemetryUnitId, DateTime startTimestamp)
    {
        var telemetryUnit = await _telemetryUnitRepository.GetByIdAsync(telemetryUnitId);

        if (telemetryUnit == null)
        {
            return null;
        }

        var (vehicleId, driverId) = await ResolveVehicleAndDriverAsync(telemetryUnit);

        var trip = new Trip
        {
            TelemetryUnitId = telemetryUnitId,
            VehicleId = vehicleId,
            DriverId = driverId,
            StartTimestamp = startTimestamp,
            EndTimestamp = null,
        };

        await _tripRepository.CreateAsync(trip);

        return trip;
    }

    // Ergänzt den EndTimestamp des offenen Trips zur TelemetryUnit.
    // Falls kein offener Trip existiert (z.B. weil der StartTrip-Call zuvor
    // fehlgeschlagen ist) und ein startTimestamp mitgegeben wurde, wird der
    // Trip stattdessen direkt mit Start- und Endzeitpunkt angelegt.
    // Gibt null zurück, wenn weder ein offener Trip existiert noch ein
    // startTimestamp vorhanden ist, oder die TelemetryUnit nicht existiert.
    public async Task<Trip?> EndTripAsync(Guid telemetryUnitId, DateTime endTimestamp, DateTime? startTimestamp = null)
    {
        var openTrip = await _tripRepository.GetOpenTripByTelemetryUnitIdAsync(telemetryUnitId);

        if (openTrip != null)
        {
            await _tripRepository.UpdateEndTimestampAsync(openTrip.Id, endTimestamp);
            openTrip.EndTimestamp = endTimestamp;

            return openTrip;
        }

        if (startTimestamp == null)
        {
            return null;
        }

        var telemetryUnit = await _telemetryUnitRepository.GetByIdAsync(telemetryUnitId);

        if (telemetryUnit == null)
        {
            return null;
        }

        var (vehicleId, driverId) = await ResolveVehicleAndDriverAsync(telemetryUnit);

        var trip = new Trip
        {
            TelemetryUnitId = telemetryUnitId,
            VehicleId = vehicleId,
            DriverId = driverId,
            StartTimestamp = startTimestamp.Value,
            EndTimestamp = endTimestamp,
        };

        await _tripRepository.CreateAsync(trip);

        return trip;
    }
}
