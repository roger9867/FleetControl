using FleetControlServer.Domain;

namespace FleetControlServer.Data.Repos;

// Datenzugriff für Fahrer inklusive ihrer Führerscheine
public interface IVehicleDriverRepository
{
    // Liefert alle vorhandenen Fahrer
    public Task<IEnumerable<VehicleDriver>> GetAllAsync();

    // Legt einen neuen Fahrer an oder aktualisiert einen bestehenden
    public Task<(bool Success, string? Error)> UpsertAsync(VehicleDriver entity);

    // Löscht den Fahrer mit der angegebenen Id
    public Task DeleteAsync(Guid id);

    // Liefert den Fahrer mit der angegebenen Id oder eine Fehlermeldung
    public Task<(bool Success, VehicleDriver? Person, string? Error)> GetByIdAsync(Guid id);
}
