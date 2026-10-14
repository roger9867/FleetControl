using IngestionService.Assignments;

namespace IngestionService;

// Entry point of the processing chain: loads the assignment cache at startup
// and then connects the MQTT client.
public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly MqttClientService _mqttClientService;
    private readonly AssignmentCache _assignmentCache;


    public Worker(
        ILogger<Worker> logger,
        MqttClientService mqttClientService,
        AssignmentCache assignmentCache
    ) {
        _logger = logger;
        _mqttClientService = mqttClientService;
        _assignmentCache = assignmentCache;
    }


    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken
    ) {
        // The assignment cache must be loaded before the MQTT client, so that
        // incoming data points are not discarded as "unknown device"
        await _assignmentCache.LoadAsync(stoppingToken);

        // Establish MQTT connection
        await _mqttClientService.StartAsync(stoppingToken);


        // Pure keep-alive loop so that the BackgroundService keeps running
        while (!stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation(
                "Worker running at: {time}",
                DateTimeOffset.Now);

            await Task.Delay(
                1000,
                stoppingToken);
        }
    }
}
