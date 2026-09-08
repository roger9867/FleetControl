using FleetControlServer.Service.Grpc;
using Grpc.Net.Client;
using Microsoft.Extensions.Configuration;

namespace FleetControlServer.Service.Assignments;

// gRPC client for IngestionService — pusht eine geänderte T-Einheit<->Fahrzeug/
// Fahrer-Zuordnung, sobald sie sich ändert, damit IngestionServices Cache
// (der jeden InfluxDB-Datenpunkt taggt) nicht bis zum nächsten eigenen
// Reload veraltet bleibt. Mirrors TelemetryQueryClient's Verbindungsaufbau.
public class IngestionServiceClient
{
    private readonly AssignmentUpdateService.AssignmentUpdateServiceClient _client;

    public IngestionServiceClient(IConfiguration configuration)
    {
        AppContext.SetSwitch(
            "System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport",
            true);

        var channel = GrpcChannel.ForAddress(
            configuration["IngestionService:Address"]!);

        _client = new AssignmentUpdateService.AssignmentUpdateServiceClient(channel);
    }

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
