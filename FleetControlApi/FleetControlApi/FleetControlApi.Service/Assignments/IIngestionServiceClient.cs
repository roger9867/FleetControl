namespace FleetControlApi.Service.Assignments;

// Pushes assignment changes to IngestionService (implemented in Gateway via gRPC)
public interface IIngestionServiceClient
{
    // Sends a changed assignment of telemetry unit to vehicle/license plate/driver to IngestionService
    Task PushAssignmentUpdateAsync(Guid telemetryUnitId, string? vehicleId, string? licensePlate, Guid? driverId);
}
