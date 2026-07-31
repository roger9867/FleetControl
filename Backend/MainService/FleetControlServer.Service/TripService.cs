using FleetControlServer.Domain;
using FleetControlServer.Data.Repos;

namespace FleetControlServer.Service;

public class TripService
{
    private readonly ITripRepository _tripRepository;
    private readonly ITelemetryUnitRepository _telemetryUnitRepository;

    public TripService(
        ITripRepository tripRepository,
        ITelemetryUnitRepository telemetryUnitRepository
        )
    {
        _tripRepository = tripRepository;
        _telemetryUnitRepository = telemetryUnitRepository;
    }

    // Legt einen neuen Trip an, sofern die referenzierte TelemetryUnit existiert.
    // Gibt null zurück, wenn die TelemetryUnit nicht existiert.
    public async Task<Trip?> StartTripAsync(Guid telemetryUnitId, DateTime startTimestamp)
    {
        var telemetryUnitExists = await _telemetryUnitRepository.ExistsAsync(telemetryUnitId);

        if (!telemetryUnitExists)
        {
            return null;
        }

        var trip = new Trip
        {
            TelemetryUnitId = telemetryUnitId,
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

        var telemetryUnitExists = await _telemetryUnitRepository.ExistsAsync(telemetryUnitId);

        if (!telemetryUnitExists)
        {
            return null;
        }

        var trip = new Trip
        {
            TelemetryUnitId = telemetryUnitId,
            StartTimestamp = startTimestamp.Value,
            EndTimestamp = endTimestamp,
        };

        await _tripRepository.CreateAsync(trip);

        return trip;
    }
}
