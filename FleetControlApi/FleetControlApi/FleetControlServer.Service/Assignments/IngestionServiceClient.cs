using FleetControlServer.Service.Grpc;
using Grpc.Net.Client;
using Microsoft.Extensions.Configuration;

namespace FleetControlServer.Service.Assignments;

// gRPC client for IngestionService
// Pusht eine geänderte Zuordnungen
public class IngestionServiceClient
{
    private readonly AssignmentUpdateService.AssignmentUpdateServiceClient _client;

    // Baut den gRPC-Kanal zu IngestionService auf
    public IngestionServiceClient(IConfiguration configuration)
    {
        AppContext.SetSwitch(
            "System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport",
            true);

        var channel = GrpcChannel.ForAddress(
            configuration["IngestionService:Address"]!);

        _client = new AssignmentUpdateService.AssignmentUpdateServiceClient(channel);
    }

    // Sendet eine geänderte Zuordnung von Telemetrie-Einheit zu Fahrzeug/Kennzeichen/Fahrer an IngestionService
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
