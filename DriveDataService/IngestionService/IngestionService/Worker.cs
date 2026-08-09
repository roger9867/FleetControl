using IngestionService.Assignments;

namespace IngestionService;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly MqttClientService _mqttClientService;
    private readonly AssignmentCache _assignmentCache;


    public Worker(
        ILogger<Worker> logger,
        MqttClientService mqttClientService,
        AssignmentCache assignmentCache)
    {
        _logger = logger;
        _mqttClientService = mqttClientService;
        _assignmentCache = assignmentCache;
    }


    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        await _assignmentCache.LoadAsync(stoppingToken);
        await _mqttClientService.StartAsync(stoppingToken);


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