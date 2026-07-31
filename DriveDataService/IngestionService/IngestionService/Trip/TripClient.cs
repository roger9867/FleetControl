using Google.Protobuf.WellKnownTypes;
using Grpc.Net.Client;

namespace IngestionService.Trip;

public class TripClient
{
    private readonly TripService.TripServiceClient _client;

    public TripClient(
        IConfiguration configuration)
    {
        // Grpc.Net.Client verweigert HTTP/2 über Klartext-HTTP (kein TLS/ALPN)
        // standardmäßig - MainService läuft ohne TLS, daher muss das hier
        // explizit erlaubt werden.
        AppContext.SetSwitch(
            "System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport",
            true);

        var channel =
            GrpcChannel.ForAddress(
                configuration["TripService:Address"]!);

        _client = new TripService.TripServiceClient(channel);
    }

    public async Task StartTripAsync(
        string deviceId,
        DateTime timestamp)
    {
        await _client.StartTripAsync(
            new StartTripRequest
            {
                TelemetryUnitId = deviceId,
                StartTimestamp = ToProtoTimestamp(timestamp)
            });
    }

    public async Task EndTripAsync(
        string deviceId,
        DateTime timestamp,
        DateTime? startTimestamp = null)
    {
        var request = new EndTripRequest
        {
            TelemetryUnitId = deviceId,
            EndTimestamp = ToProtoTimestamp(timestamp)
        };

        if (startTimestamp != null)
        {
            request.StartTimestamp = ToProtoTimestamp(startTimestamp.Value);
        }

        await _client.EndTripAsync(request);
    }

    private static Timestamp ToProtoTimestamp(
        DateTime timestamp)
    {
        return Timestamp.FromDateTime(
            DateTime.SpecifyKind(timestamp, DateTimeKind.Utc));
    }
}
