using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.SignalR;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace FleetControlServer.Api.Realtime;

// Verbindet sich mit RabbitMQ-Broker, an den
// IngestionService jeden verarbeiteten Datenpunkt
// veröffentlicht, und reicht "Fahrzeug bewegt sich"-Events an alle
// verbundenen Browser-Clients ueber den VehicleHub weiter
public class RabbitMqTelemetryConsumer : BackgroundService
{
    private readonly IConfiguration _configuration;
    private readonly IHubContext<VehicleHub> _hub;
    private readonly ILogger<RabbitMqTelemetryConsumer> _logger;

    private string Exchange => _configuration["RabbitMq:Exchange"] ?? "telemetry-events";

    // Erstellt den Consumer und injiziert Konfiguration, SignalR-Hub-Context und Logger
    public RabbitMqTelemetryConsumer(
        IConfiguration configuration,
        IHubContext<VehicleHub> hub,
        ILogger<RabbitMqTelemetryConsumer> logger
    ) {
        _configuration = configuration;
        _hub = hub;
        _logger = logger;
    }

    // Hält die Verbindung zu RabbitMQ offen und verbindet nach einem Abbruch automatisch neu
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ConsumeUntilDisconnectedAsync(stoppingToken);
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogWarning(
                    ex,
                    "RabbitMQ-Verbindung fuer Live-Updates unterbrochen, naechster Versuch in 5s");

                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
    }

    // Baut die RabbitMQ-Verbindung auf, abonniert Telemetrie-Events und leitet sie per
    // SignalR an alle verbundenen Clients weiter, bis die Verbindung abbricht oder gestoppt wird
    private async Task ConsumeUntilDisconnectedAsync(CancellationToken stoppingToken)
    {
        var factory = new ConnectionFactory
        {
            HostName = _configuration["RabbitMq:Host"] ?? "localhost",
            Port = int.Parse(_configuration["RabbitMq:Port"] ?? "5672"),
            UserName = _configuration["RabbitMq:Username"] ?? "guest",
            Password = _configuration["RabbitMq:Password"] ?? "guest"
        };

        await using var connection = await factory.CreateConnectionAsync(stoppingToken);
        await using var channel = await connection.CreateChannelAsync(cancellationToken: stoppingToken);

        await channel.ExchangeDeclareAsync(
            exchange: Exchange,
            type: ExchangeType.Topic,
            durable: true,
            cancellationToken: stoppingToken);

        // Queue
        var queueDeclareResult = await channel.QueueDeclareAsync(
            queue: string.Empty,
            durable: false,
            exclusive: true,
            autoDelete: true,
            cancellationToken: stoppingToken);

        await channel.QueueBindAsync(
            queue: queueDeclareResult.QueueName,
            exchange: Exchange,
            routingKey: "telemetry.#",
            cancellationToken: stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(channel);

        consumer.ReceivedAsync += async (_, ea) =>
        {
            try
            {
                var json = Encoding.UTF8.GetString(ea.Body.Span);
                var message = JsonSerializer.Deserialize<TelemetryEventMessage>(json);

                if (!string.IsNullOrEmpty(message?.VehicleId))
                {
                    await _hub.Clients.All.SendAsync(
                        "VehicleUpdate",
                        new VehicleUpdateMessage(
                            message.VehicleId,
                            message.DeviceId,
                            message.Lat,
                            message.Lon,
                            message.SpeedKmh,
                            message.AccelMs2,
                            message.Timestamp,
                            message.DriverId),
                        stoppingToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Telemetry-Event konnte nicht verarbeitet werden");
            }
        };

        await channel.BasicConsumeAsync(
            queue: queueDeclareResult.QueueName,
            autoAck: true,
            consumer: consumer,
            cancellationToken: stoppingToken);

        _logger.LogInformation(
            "RabbitMQ-Live-Consumer verbunden ({host}:{port}, Exchange '{exchange}')",
            factory.HostName,
            factory.Port,
            Exchange);

        // Wartet, bis entweder der Host stoppt oder die Verbindung abbricht,
        // erst dann greift die Retry-Schleife in ExecuteAsync erneut.
        var disconnected = new TaskCompletionSource();

        connection.ConnectionShutdownAsync += (_, __) =>
        {
            disconnected.TrySetResult();
            return Task.CompletedTask;
        };

        await using var registration = stoppingToken.Register(() => disconnected.TrySetResult());

        await disconnected.Task;
        stoppingToken.ThrowIfCancellationRequested();
    }
}
