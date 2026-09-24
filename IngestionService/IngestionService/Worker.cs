using IngestionService.Assignments;

namespace IngestionService;

// Einstiegspunkt der Verarbeitungskette: lädt beim Start den Zuordnungs-Cache
// und verbindet danach MQTT-Client.
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
        // Zuordnungs-Cache muss vor dem MQTT-Client geladen werden, damit
        // eintreffende Datenpunkte nicht als "unbekanntes Gerät" verworfen werden
        await _assignmentCache.LoadAsync(stoppingToken);

        // Aufbau MQTT-Verbindung
        await _mqttClientService.StartAsync(stoppingToken);


        // Reiner Lebenszeichen-Loop, damit der BackgroundService am laufen bleibt
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
