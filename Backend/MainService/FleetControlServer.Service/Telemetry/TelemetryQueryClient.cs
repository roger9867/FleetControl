using FleetControlServer.Service.Grpc;
using Google.Protobuf.WellKnownTypes;
using Grpc.Net.Client;
using Microsoft.Extensions.Configuration;

namespace FleetControlServer.Service.Telemetry;

public record TelemetryPointResult(
    DateTime Timestamp,
    double Lat,
    double Lon,
    double SpeedKmh,
    double AccelMs2);

// gRPC client for TelemetryDataService — mirrors IngestionService's TripClient
// pattern (same unencrypted-HTTP/2 workaround, since MainService/TelemetryDataService
// both run without TLS locally).
public class TelemetryQueryClient
{
    private readonly TelemetryQuery.TelemetryQueryClient _client;

    public TelemetryQueryClient(IConfiguration configuration)
    {
        AppContext.SetSwitch(
            "System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport",
            true);

        var channel = GrpcChannel.ForAddress(
            configuration["TelemetryDataService:Address"]!);

        _client = new TelemetryQuery.TelemetryQueryClient(channel);
    }

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

    private static Timestamp ToProtoTimestamp(DateTime timestamp)
    {
        return Timestamp.FromDateTime(
            DateTime.SpecifyKind(timestamp, DateTimeKind.Utc));
    }
}
