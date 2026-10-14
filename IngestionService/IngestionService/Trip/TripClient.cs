using Google.Protobuf.WellKnownTypes;
using Grpc.Net.Client;

namespace IngestionService.Trip;

// Wraps the generated gRPC client for FleetControlApi's TripService,
// through which trip starts and ends are reported
public class TripClient
{
    private readonly TripService.TripServiceClient _client;

    public TripClient(
        IConfiguration configuration
    ) {

        AppContext.SetSwitch(
            "System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport",
            true);

        var channel =
            GrpcChannel.ForAddress(
                configuration["TripService:Url"]!);

        _client = new TripService.TripServiceClient(channel);
    }

    // Reports the trip start detected by the state machine (device ID, start time) to FleetControlApi
    public async Task StartTripAsync(
        string deviceId,
        DateTime timestamp
    ) {
        await _client.StartTripAsync(
            new StartTripRequest
            {
                TelemetryUnitId = deviceId,
                StartTimestamp = ToProtoTimestamp(timestamp)
            });
    }

    // Reports the detected trip end to FleetControlApi.
    public async Task EndTripAsync(
        string deviceId,
        DateTime timestamp,
        DateTime? startTimestamp = null
    ) {
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

    // Converts a timestamp into the Timestamp format expected by Protocol Buffers
    private static Timestamp ToProtoTimestamp(
        DateTime timestamp
    ) {
        return Timestamp.FromDateTime(
            DateTime.SpecifyKind(timestamp, DateTimeKind.Utc));
    }
}
