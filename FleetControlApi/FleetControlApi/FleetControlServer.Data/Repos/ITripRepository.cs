using FleetControlServer.Domain;

namespace FleetControlServer.Data.Repos;

// Datenzugriff für Fahrten
public interface ITripRepository
{
    // Legt eine neue Fahrt an
    Task CreateAsync(Trip entity);

    // Liefert die aktuell offene (noch nicht beendete) Fahrt einer Telemetrie-Einheit, falls vorhanden
    Task<Trip?> GetOpenTripByTelemetryUnitIdAsync(Guid telemetryUnitId);

    // Liefert die Fahrt mit der angegebenen Id oder null
    Task<Trip?> GetByIdAsync(Guid id);

    // Setzt den Endzeitpunkt einer Fahrt
    Task<bool> UpdateEndTimestampAsync(Guid tripId, DateTime endTimestamp);

    // Gibt gespeicherte Fahrten seitenweise aus
    Task<(List<Trip> Trips, int TotalCount)> GetPageAsync(int page, int pageSize);

    // Löscht die Fahrt mit der angegebenen Id
    Task<bool> DeleteAsync(Guid id);

    // Prüft, ob für das angegebene Fahrzeug bereits Fahrten existieren
    Task<bool> ExistsForVehicleAsync(string vehicleId);

    // Prüft, ob für die angegebene Telemetrie-Einheit bereits Fahrten existieren
    Task<bool> ExistsForTelemetryUnitAsync(Guid telemetryUnitId);

    // Prüft, ob für den angegebenen Fahrer bereits Fahrten existieren
    Task<bool> ExistsForDriverAsync(Guid driverId);
}
