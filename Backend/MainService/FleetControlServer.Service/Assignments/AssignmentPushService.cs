using FleetControlServer.Data.Repos;
using Microsoft.Extensions.Logging;

namespace FleetControlServer.Service.Assignments;

// Zentralisiert das Best-effort-Pushen einzelner T-Einheit-Zuordnungen an
// IngestionService - genutzt von jedem Service, der eine Verknuepfung
// (T-Einheit/Fahrzeug/Fahrer) aendern kann, damit IngestionServices Cache
// nie bis zum naechsten eigenen Reload veraltet bleibt.
public class AssignmentPushService
{
    private readonly ITelemetryUnitRepository _telemetryUnitRepo;
    private readonly IngestionServiceClient _ingestionServiceClient;
    private readonly ILogger<AssignmentPushService> _logger;

    public AssignmentPushService(
        ITelemetryUnitRepository telemetryUnitRepo,
        IngestionServiceClient ingestionServiceClient,
        ILogger<AssignmentPushService> logger)
    {
        _telemetryUnitRepo = telemetryUnitRepo;
        _ingestionServiceClient = ingestionServiceClient;
        _logger = logger;
    }

    public async Task PushAsync(Guid telemetryUnitId)
    {
        try
        {
            var assignment = await _telemetryUnitRepo.GetAssignmentAsync(telemetryUnitId);

            if (assignment == null)
            {
                return;
            }

            await _ingestionServiceClient.PushAssignmentUpdateAsync(
                assignment.TelemetryUnitId,
                assignment.VehicleId,
                assignment.LicensePlateNumber,
                assignment.DriverId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Push der T-Einheit-Zuordnung {telemetryUnitId} an IngestionService fehlgeschlagen",
                telemetryUnitId);
        }
    }

    public async Task PushAsync(IEnumerable<Guid> telemetryUnitIds)
    {
        foreach (var id in telemetryUnitIds)
        {
            await PushAsync(id);
        }
    }
}
