using Grpc.Net.Client;

namespace IngestionService.Assignments;

public class AssignmentClient
{
    private readonly AssignmentService.AssignmentServiceClient _client;

    public AssignmentClient(
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
                configuration["AssignmentService:Address"]!);

        _client = new AssignmentService.AssignmentServiceClient(channel);
    }

    public async Task<IReadOnlyList<Assignment>> GetCurrentAssignmentsAsync()
    {
        var response = await _client.GetCurrentAssignmentsAsync(new GetCurrentAssignmentsRequest());

        return response.Assignments;
    }
}
