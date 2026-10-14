using Grpc.Net.Client;

namespace IngestionService.Assignments;

// Wraps the generated gRPC client for FleetControlApi's AssignmentService,
// through which the complete set of assignments is queried at startup
public class AssignmentClient
{
    private readonly AssignmentService.AssignmentServiceClient _client;

    public AssignmentClient(
        IConfiguration configuration)
    {
        // Allow HTTP/2 over plain-text HTTP, forbidden by default
        AppContext.SetSwitch(
            "System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport",
            true);

        var channel =
            GrpcChannel.ForAddress(
                configuration["AssignmentService:Url"]!);

        _client = new AssignmentService.AssignmentServiceClient(channel);
    }

    // Queries the current set of assignments from FleetControlApi
    public async Task<IReadOnlyList<Assignment>> GetCurrentAssignmentsAsync()
    {
        var response = await _client.GetCurrentAssignmentsAsync(new GetCurrentAssignmentsRequest());

        return response.Assignments;
    }
}
