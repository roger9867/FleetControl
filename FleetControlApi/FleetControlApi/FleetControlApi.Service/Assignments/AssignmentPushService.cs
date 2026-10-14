using FleetControlApi.Service.Repositories;
using Microsoft.Extensions.Logging;

namespace FleetControlApi.Service.Assignments;

// Pushes individual telemetry unit assignments to IngestionService
public class AssignmentPushService : IAssignmentPushService
{
    private readonly ITelemetryUnitRepository _telemetryUnitRepo;
    private readonly IIngestionServiceClient _ingestionServiceClient;
    private readonly ILogger<AssignmentPushService> _logger;

    public AssignmentPushService(
        ITelemetryUnitRepository telemetryUnitRepo,
        IIngestionServiceClient ingestionServiceClient,
        ILogger<AssignmentPushService> logger)
    {
        _telemetryUnitRepo = telemetryUnitRepo;
        _ingestionServiceClient = ingestionServiceClient;
        _logger = logger;
    }

    // Pushes the current assignment of a single telemetry unit to IngestionService
    public async Task PushAsync(Guid telemetryUnitId)
    {
        try
        {
            var unit = await _telemetryUnitRepo.GetByIdAsync(telemetryUnitId);

            if (unit == null)
            {
                return;
            }

            await _ingestionServiceClient.PushAssignmentUpdateAsync(
                unit.Id,
                unit.Vehicle?.VehicleIdentificationNumber,
                unit.Vehicle?.LicensePlateNumber,
                unit.Vehicle?.VehicleDriverId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Push der T-Einheit-Zuordnung {telemetryUnitId} an IngestionService fehlgeschlagen",
                telemetryUnitId);
        }
    }

    // Pushes the current assignments of several telemetry units to IngestionService one after another
    public async Task PushAsync(IEnumerable<Guid> telemetryUnitIds)
    {
        foreach (var id in telemetryUnitIds)
        {
            await PushAsync(id);
        }
    }
}
