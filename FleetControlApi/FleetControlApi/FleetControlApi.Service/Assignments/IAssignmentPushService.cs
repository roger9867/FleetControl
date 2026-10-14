namespace FleetControlApi.Service.Assignments;

// Pushes the current telemetry unit assignments to IngestionService
public interface IAssignmentPushService
{
    // Pushes the current assignment of a single telemetry unit to IngestionService
    Task PushAsync(Guid telemetryUnitId);

    // Pushes the current assignments of several telemetry units to IngestionService one after another
    Task PushAsync(IEnumerable<Guid> telemetryUnitIds);
}
