using FleetControlServer.Service.Grpc;
using Google.Protobuf.WellKnownTypes;
using Grpc.Net.Client;
using Microsoft.Extensions.Configuration;

namespace FleetControlServer.Service.Telemetry;

// Ein einzelner von TelemetryDataService gelieferter Telemetriepunkt.
public record TelemetryPointResult(
    DateTime Timestamp,
    double Lat,
    double Lon,
    double SpeedKmh,
    double AccelMs2,
    string? DriverId = null,
    string? DeviceId = null);

// gRPC client for TelemetryDataService — mirrors IngestionService's TripClient
// pattern (same unencrypted-HTTP/2 workaround, since FleetControlService/TelemetryDataService
// both run without TLS locally).
public class TelemetryQueryClient
{
    private readonly TelemetryQuery.TelemetryQueryClient _client;

    // Baut den gRPC-Kanal zu TelemetryDataService auf (unverschlüsseltes HTTP/2, lokal ohne TLS).
    public TelemetryQueryClient(IConfiguration configuration)
    {
        AppContext.SetSwitch(
            "System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport",
            true);

        var channel = GrpcChannel.ForAddress(
            configuration["TelemetryDataService:Address"]!);

        _client = new TelemetryQuery.TelemetryQueryClient(channel);
    }

    // Liefert alle Telemetriepunkte eines Geräts im angefragten Zeitfenster.
    public async Task<List<TelemetryPointResult>> GetTelemetryPointsAsync(
        string deviceId,
        DateTime start,
        DateTime end)
    {
        var response = await _client.GetTelemetryPointsAsync(
            new GetTelemetryPointsRequest
            {
                DeviceId = deviceId,
                Start = ToProtoTimestamp(start),
                End = ToProtoTimestamp(end)
            });

        return response.Points
            .Select(p => new TelemetryPointResult(
                p.Timestamp.ToDateTime(),
                p.Lat,
                p.Lon,
                p.SpeedKmh,
                p.AccelMs2))
            .ToList();
    }

    // Liefert den jeweils letzten Telemetriepunkt mehrerer Geräte, per Geräte-Id.
    public async Task<Dictionary<string, TelemetryPointResult>> GetLatestPointsAsync(
        IEnumerable<string> deviceIds)
    {
        var request = new GetLatestPointsRequest();
        request.DeviceIds.AddRange(deviceIds);

        if (request.DeviceIds.Count == 0)
        {
            return new Dictionary<string, TelemetryPointResult>();
        }

        var response = await _client.GetLatestPointsAsync(request);

        return response.Points.ToDictionary(
            p => p.DeviceId,
            p => new TelemetryPointResult(
                p.Point.Timestamp.ToDateTime(),
                p.Point.Lat,
                p.Point.Lon,
                p.Point.SpeedKmh,
                p.Point.AccelMs2),
            StringComparer.OrdinalIgnoreCase);
    }

    // Liefert den jeweils letzten Telemetriepunkt mehrerer Fahrzeuge, per Fahrzeug-Id.
    public async Task<Dictionary<string, TelemetryPointResult>> GetLatestPointsByVehicleAsync(
        IEnumerable<string> vehicleIds)
    {
        var request = new GetLatestPointsByVehicleRequest();
        request.VehicleIds.AddRange(vehicleIds);

        if (request.VehicleIds.Count == 0)
        {
            return new Dictionary<string, TelemetryPointResult>();
        }

        var response = await _client.GetLatestPointsByVehicleAsync(request);

        return response.Points.ToDictionary(
            p => p.VehicleId,
            p => new TelemetryPointResult(
                p.Point.Timestamp.ToDateTime(),
                p.Point.Lat,
                p.Point.Lon,
                p.Point.SpeedKmh,
                p.Point.AccelMs2,
                string.IsNullOrEmpty(p.Point.DriverId) ? null : p.Point.DriverId,
                string.IsNullOrEmpty(p.Point.DeviceId) ? null : p.Point.DeviceId));
    }

    // Löscht alle Telemetriepunkte eines Geräts im angegebenen Zeitfenster.
    public async Task<bool> DeleteTelemetryPointsAsync(
        string deviceId,
        DateTime start,
        DateTime end)
    {
        var response = await _client.DeleteTelemetryPointsAsync(
            new DeleteTelemetryPointsRequest
            {
                DeviceId = deviceId,
                Start = ToProtoTimestamp(start),
                End = ToProtoTimestamp(end)
            });

        return response.Success;
    }

    // Wandelt einen DateTime-Wert in einen UTC-Protobuf-Timestamp um.
    private static Timestamp ToProtoTimestamp(DateTime timestamp)
    {
        return Timestamp.FromDateTime(
            DateTime.SpecifyKind(timestamp, DateTimeKind.Utc));
    }
}
