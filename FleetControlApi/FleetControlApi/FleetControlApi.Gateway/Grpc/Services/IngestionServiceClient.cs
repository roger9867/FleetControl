using FleetControlApi.Service.Assignments;

namespace FleetControlApi.Gateway.Grpc.Services;

// gRPC client for IngestionService
// Pushes a changed assignment
public class IngestionServiceClient : IIngestionServiceClient
{
    private readonly AssignmentUpdateService.AssignmentUpdateServiceClient _client;

    // The generated gRPC client is created and configured by DI (AddGrpcClient in Program.cs)
    public IngestionServiceClient(AssignmentUpdateService.AssignmentUpdateServiceClient client)
    {
        _client = client;
    }

    // Sends a changed assignment of telemetry unit to vehicle/license plate/driver to IngestionService
    public async Task PushAssignmentUpdateAsync(
        Guid telemetryUnitId,
        string? vehicleId,
        string? licensePlate,
        Guid? driverId)
    {
        var update = new AssignmentUpdate
        {
            TelemetryUnitId = telemetryUnitId.ToString()
        };

        if (vehicleId != null) update.VehicleId = vehicleId;
        if (licensePlate != null) update.LicensePlate = licensePlate;
        if (driverId != null) update.DriverId = driverId.Value.ToString();

        await _client.PushAssignmentUpdateAsync(update);
    }
}
