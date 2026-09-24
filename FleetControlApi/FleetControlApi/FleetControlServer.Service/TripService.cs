using FleetControlServer.Domain;
using FleetControlServer.Data.Repos;
using FleetControlServer.Service.DTO.Trip;
using FleetControlServer.Service.Telemetry;
using Microsoft.Extensions.Logging;

namespace FleetControlServer.Service;

// Logik für Fahrten: Starten, Beenden, seitenweises Abfragen inklusive Telemetriepunkten, Löschen
public class TripService
{
    private const int MaxPageSize = 10;

    private readonly ITripRepository _tripRepository;
    private readonly ITelemetryUnitRepository _telemetryUnitRepository;
    private readonly IVehicleRepository _vehicleRepository;
    private readonly TelemetryQueryClient _telemetryQueryClient;
    private readonly ILogger<TripService> _logger;

    public TripService(
        ITripRepository tripRepository,
        ITelemetryUnitRepository telemetryUnitRepository,
        IVehicleRepository vehicleRepository,
        TelemetryQueryClient telemetryQueryClient,
        ILogger<TripService> logger
        )
    {
        _tripRepository = tripRepository;
        _telemetryUnitRepository = telemetryUnitRepository;
        _vehicleRepository = vehicleRepository;
        _telemetryQueryClient = telemetryQueryClient;
        _logger = logger;
    }

    // Lädt immer höchstens eine Seite mit MaxPageSize Fahrten und holt dann deren
    // InfluxDB-Punkte per gRPC von TelemetryDataService (ein Call pro Fahrt, parallel),
    // damit Karte/Liste nie mehr als 10 Fahrten Telemetrie auf einmal laden müssen
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

    // Baut das TripDto für eine Fahrt inklusive ihrer per gRPC geladenen InfluxDB-Punkte
    private async Task<TripDto> BuildTripDtoAsync(Trip trip)
    {
        var start = DateTime.SpecifyKind(trip.StartTimestamp, DateTimeKind.Utc);
        var end = DateTime.SpecifyKind(trip.EndTimestamp ?? DateTime.UtcNow, DateTimeKind.Utc);

        var points = await _telemetryQueryClient.GetTelemetryPointsAsync(
            trip.TelemetryUnitId.ToString(),
            start,
            end);

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

    // Ermittelt das aktuell mit einer TelemetryUnit verknüpfte Vehicle (und dessen Fahrer)
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

    // Legt einen neuen Trip an, sofern die referenzierte TelemetryUnit existiert
    // Gibt null zurück, wenn die TelemetryUnit nicht existiert
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

    // Löscht eine abgeschlossene Fahrt inklusive ihrer InfluxDB-Telemetriepunkte
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
