using FleetControlApi.Domain;
using FleetControlApi.Service.Repositories;
using FleetControlApi.Service.Telemetry;
using Microsoft.Extensions.Logging;

namespace FleetControlApi.Service;

// Logic for trips: start, end, paged queries including telemetry points, delete
public class TripService
{
    private const int MaxPageSize = 10;

    private readonly ITripRepository _tripRepository;
    private readonly ITelemetryUnitRepository _telemetryUnitRepository;
    private readonly IVehicleRepository _vehicleRepository;
    private readonly ITelemetryQueryClient _telemetryQueryClient;
    private readonly ILogger<TripService> _logger;

    public TripService(
        ITripRepository tripRepository,
        ITelemetryUnitRepository telemetryUnitRepository,
        IVehicleRepository vehicleRepository,
        ITelemetryQueryClient telemetryQueryClient,
        ILogger<TripService> logger
        )
    {
        _tripRepository = tripRepository;
        _telemetryUnitRepository = telemetryUnitRepository;
        _vehicleRepository = vehicleRepository;
        _telemetryQueryClient = telemetryQueryClient;
        _logger = logger;
    }

    // Always loads at most one page of MaxPageSize trips and then fetches their
    // InfluxDB points via gRPC from TelemetryDataService (one call per trip, in parallel),
    // so that the map/list never has to load telemetry for more than 10 trips at once
    public async Task<(List<Trip> Trips, int TotalCount)> GetPageAsync(int page, int pageSize)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, MaxPageSize);

        var (trips, totalCount) = await _tripRepository.GetPageAsync(page, pageSize);

        await Task.WhenAll(trips.Select(LoadPointsAsync));

        return (trips, totalCount);
    }

    // Loads the InfluxDB points of a trip via gRPC into the trip
    private async Task LoadPointsAsync(Trip trip)
    {
        var start = DateTime.SpecifyKind(trip.StartTimestamp, DateTimeKind.Utc);
        var end = DateTime.SpecifyKind(trip.EndTimestamp ?? DateTime.UtcNow, DateTimeKind.Utc);

        trip.Points = await _telemetryQueryClient.GetTelemetryPointsAsync(
            trip.TelemetryUnitId.ToString(),
            start,
            end);
    }

    // Determines the vehicle (and its driver) currently linked to a TelemetryUnit
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

    // Creates a new trip if the referenced TelemetryUnit exists
    // Returns null if the TelemetryUnit does not exist
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

    // Sets the EndTimestamp of the open trip of the TelemetryUnit.
    // If no open trip exists (e.g. because the StartTrip call previously
    // failed) and a startTimestamp was passed, the
    // trip is instead created directly with start and end time.
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

    // Deletes a completed trip including its InfluxDB telemetry points
    public async Task<(bool Success, string? Error)> DeleteAsync(Guid id)
    {
        var trip = await _tripRepository.GetByIdAsync(id);

        if (trip == null)
        {
            return (false, "Fahrt nicht gefunden.");
        }

        if (trip.EndTimestamp == null)
        {
            return (false, "Laufende Fahrten können nicht gelöscht werden.");
        }

        try
        {
            await _telemetryQueryClient.DeleteTelemetryPointsAsync(
                trip.TelemetryUnitId.ToString(),
                trip.StartTimestamp,
                trip.EndTimestamp.Value);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Failed to delete InfluxDB points for trip {tripId} (telemetry unit {telemetryUnitId})",
                id,
                trip.TelemetryUnitId);
        }

        await _tripRepository.DeleteAsync(id);

        return (true, null);
    }
}
