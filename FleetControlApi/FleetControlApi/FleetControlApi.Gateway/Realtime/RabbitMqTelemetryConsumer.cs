using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.SignalR;
using FleetControlApi.Gateway.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace FleetControlApi.Gateway.Realtime;

// Connects to the RabbitMQ broker to which
// IngestionService publishes every processed
// data point, and forwards "vehicle is moving" events to all
// connected browser clients via the VehicleHub
public class RabbitMqTelemetryConsumer : BackgroundService
{
    private readonly IConfiguration _configuration;
    private readonly IConnectionFactory _connectionFactory;
    private readonly IHubContext<VehicleHub> _hub;
    private readonly ILogger<RabbitMqTelemetryConsumer> _logger;

    private string Exchange => _configuration["RabbitMq:Exchange"] ?? "telemetry-events";

    // Creates the consumer and injects configuration, RabbitMQ connection factory, SignalR hub context and logger
    public RabbitMqTelemetryConsumer(
        IConfiguration configuration,
        IConnectionFactory connectionFactory,
        IHubContext<VehicleHub> hub,
        ILogger<RabbitMqTelemetryConsumer> logger
    ) {
        _configuration = configuration;
        _connectionFactory = connectionFactory;
        _hub = hub;
        _logger = logger;
    }

    // Keeps the connection to RabbitMQ open and reconnects automatically after a disconnect
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

    // Establishes the RabbitMQ connection, subscribes to telemetry events and forwards them via
    // SignalR to all connected clients until the connection drops or is stopped
    private async Task ConsumeUntilDisconnectedAsync(CancellationToken stoppingToken)
    {
        await using var connection = await _connectionFactory.CreateConnectionAsync(stoppingToken);
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
            connection.Endpoint.HostName,
            connection.Endpoint.Port,
            Exchange);

        // Waits until either the host stops or the connection drops,
        // only then does the retry loop in ExecuteAsync kick in again.
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
