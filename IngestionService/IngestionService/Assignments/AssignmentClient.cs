using Grpc.Net.Client;

namespace IngestionService.Assignments;

// Kapselt den generierten gRPC-Client für AssignmentService von FleetControlApi,
// über den der komplette Zuordnungsbestand beim Start abgefragt wird
public class AssignmentClient
{
    private readonly AssignmentService.AssignmentServiceClient _client;

    public AssignmentClient(
        IConfiguration configuration)
    {
        // Erlauben von HTTP/2 über Klartext-HTTP, standardmäßig verboten
        AppContext.SetSwitch(
            "System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport",
            true);

        var channel =
            GrpcChannel.ForAddress(
                configuration["AssignmentService:Address"]!);

        _client = new AssignmentService.AssignmentServiceClient(channel);
    }

    // Fragt bei FleetControlApi den aktuellen Zuordnungsbestand ab
    public async Task<IReadOnlyList<Assignment>> GetCurrentAssignmentsAsync()
    {
        var response = await _client.GetCurrentAssignmentsAsync(new GetCurrentAssignmentsRequest());

        return response.Assignments;
    }
}
