using Google.Protobuf.WellKnownTypes;
using Grpc.Net.Client;

namespace IngestionService.Trip;

// Kapselt den generierten gRPC-Client für den TripService von FleetControlApi,
// über den Fahrtbeginn und -ende gemeldet werden
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
                configuration["TripService:Address"]!);

        _client = new TripService.TripServiceClient(channel);
    }

    // Meldet den vom Zustandsautomaten erkannten Fahrtbeginn (Geräte-ID, Startzeitpunkt) an FleetControlApi
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

    // Meldet das erkannte Fahrtende an FleetControlApi.
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

    // Wandelt einen Zeitstempel in das von Protocol Buffers erwartete Timestamp-Format um
    private static Timestamp ToProtoTimestamp(
        DateTime timestamp
    ) {
        return Timestamp.FromDateTime(
            DateTime.SpecifyKind(timestamp, DateTimeKind.Utc));
    }
}
