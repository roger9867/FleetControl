namespace IngestionService;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly MqttClientService _mqttClientService;


    public Worker(
        ILogger<Worker> logger,
        MqttClientService mqttClientService)
    {
        _logger = logger;
        _mqttClientService = mqttClientService;
    }


    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
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