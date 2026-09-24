using FleetControlServer.Domain;

namespace FleetControlServer.Data.Repos;

// Datenzugriff für Telemetrie-Einheiten und deren Zuordnung zu Fahrzeugen
public interface ITelemetryUnitRepository
{
    // Legt eine neue Telemetrie-Einheit an
    Task CreateAsync(TelemetryUnit entity);

    // Liefert alle vorhandenen Telemetrie-Einheiten.
    Task<List<TelemetryUnit>> GetAllAsync();

    // Prüft, ob eine Telemetrie-Einheit mit der angegebenen Id existiert
    Task<bool> ExistsAsync(Guid id);

    // Liefert die Telemetrie-Einheit mit der angegebenen Id oder null
    Task<TelemetryUnit?> GetByIdAsync(Guid id);

    // Löscht die Telemetrie-Einheit mit der angegebenen Id
    Task<bool> DeleteAsync(Guid id);

    // Setzt oder entfernt die Fahrzeug-Zuordnung einer Telemetrie-Einheit
    Task<bool> SetVehicleAsync(Guid telemetryUnitId, string? vehicleId);

    // Entfernt die Fahrzeug-Zuordnung von allen Telemetrie-Einheiten, die dem angegebenen Fahrzeug zugeordnet sind
    Task ClearVehicleAsync(string vehicleId);

    // Liefert die aktuellen Zuordnungen aller Telemetrie-Einheiten zu Fahrzeug, Kennzeichen und Fahrer
    Task<List<TelemetryAssignment>> GetCurrentAssignmentsAsync();

    // Liefert die aktuelle Zuordnung einer einzelnen Telemetrie-Einheit
    Task<TelemetryAssignment?> GetAssignmentAsync(Guid telemetryUnitId);
}
