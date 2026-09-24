using FleetControlServer.Domain;

namespace FleetControlServer.Data.Repos;

// Datenzugriff für Fahrzeuge
public interface IVehicleRepository
{
    // Liefert alle vorhandenen Fahrzeuge
    public Task<IEnumerable<Vehicle>> GetAllAsync();

    // Legt ein neues Fahrzeug an oder aktualisiert ein bestehendes
    public Task<(bool Success, string? Error)> UpsertAsync(Vehicle entity);

    // Löscht das angegebene Fahrzeug
    public Task DeleteAsync(Vehicle entity);

    // Liefert das Fahrzeug mit der angegebenen Id oder eine Fehlermeldung
    public Task<(bool Success, Vehicle? Vehicle, string? Error)> GetByIdAsync(string id);

    // Prüft, ob ein Fahrer mit der angegebenen Id existiert
    public Task<bool> DriverExistsAsync(Guid id);

    // Setzt oder entfernt den zugeordneten Fahrer eines Fahrzeugs
    public Task<bool> SetDriverAsync(string vehicleId, Guid? driverId);

    // Entfernt einen Fahrer von allen Fahrzeugen außer dem angegebenen, damit ein Fahrer nur einem Fahrzeug zugeordnet ist
    public Task ClearDriverFromOtherVehiclesAsync(Guid driverId, string? exceptVehicleId);
}
