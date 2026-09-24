using FleetControlServer.Data.Repos;
using Microsoft.Extensions.Logging;

namespace FleetControlServer.Service.Assignments;

// Pushen einzelner T-Einheit-Zuordnungen an IngestionService
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

    // Pusht die aktuelle Zuordnung einer einzelnen Telemetrie-Einheit an IngestionService
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

    // Pusht die aktuellen Zuordnungen mehrerer Telemetrie-Einheiten nacheinander an IngestionService
    public async Task PushAsync(IEnumerable<Guid> telemetryUnitIds)
    {
        foreach (var id in telemetryUnitIds)
        {
            await PushAsync(id);
        }
    }
}
