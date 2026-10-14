using System.Text.Json;
using RabbitMQ.Client;

namespace IngestionService.RabbitMq;

// Sends events for live map updates of the UI via RabbitMQ
// If RabbitMQ is not reachable, the event is discarded
// and the next data point tries to re-establish the connection
public class RabbitMqPublisher : IAsyncDisposable
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<RabbitMqPublisher> _logger;
    private readonly SemaphoreSlim _connectLock = new(1, 1);

    private IConnection? _connection;
    private IChannel? _channel;

    private string Exchange => _configuration["RabbitMq:Exchange"] ?? "telemetry-events";

    public RabbitMqPublisher(
        IConfiguration configuration,
        ILogger<RabbitMqPublisher> logger
    ) {
        _configuration = configuration;
        _logger = logger;
    }

    // Serializes the payload to JSON and sends it
    // If publishing fails, the connection is discarded instead of retrying
    public async Task PublishAsync(
        string routingKey,
        object payload,
        CancellationToken token = default
    ) {
        var channel = await GetChannelAsync(token);

        if (channel == null)
        {
            return;
        }

        try
        {
            var body = JsonSerializer.SerializeToUtf8Bytes(payload);

            await channel.BasicPublishAsync(
                exchange: Exchange,
                routingKey: routingKey,
                mandatory: false,
                basicProperties: new BasicProperties { ContentType = "application/json" },
                body: body,
                cancellationToken: token);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "RabbitMQ-Publish fehlgeschlagen für Routing-Key {routingKey}",
                routingKey);

            await ResetConnectionAsync();
        }
    }

    // Returns the existing open channel or, if needed, re-establishes
    // connection and channel.
    // Returns null if RabbitMQ is not reachable
    private async Task<IChannel?> GetChannelAsync(CancellationToken token)
    {
        if (_channel is { IsOpen: true })
        {
            return _channel;
        }

        await _connectLock.WaitAsync(token);

        try
        {
            if (_channel is { IsOpen: true })
            {
                return _channel;
            }

            var factory = new ConnectionFactory
            {
                HostName = _configuration["RabbitMq:Host"] ?? "localhost",
                Port = int.Parse(_configuration["RabbitMq:Port"] ?? "5672"),
                UserName = _configuration["RabbitMq:Username"] ?? "guest",
                Password = _configuration["RabbitMq:Password"] ?? "guest"
            };

            _connection = await factory.CreateConnectionAsync(token);
            _channel = await _connection.CreateChannelAsync(cancellationToken: token);

            await _channel.ExchangeDeclareAsync(
                exchange: Exchange,
                type: ExchangeType.Topic,
                durable: true,
                cancellationToken: token);

            _logger.LogInformation(
                "RabbitMQ-Verbindung zu {host}:{port} hergestellt (Exchange '{exchange}')",
                factory.HostName,
                factory.Port,
                Exchange);

            return _channel;
        }
        catch (Exception ex) when (!token.IsCancellationRequested)
        {
            _logger.LogWarning(
                ex,
                "RabbitMQ-Verbindung fehlgeschlagen - Event wird verworfen, nächster Versuch beim nächsten Datenpunkt");

            return null;
        }
        finally
        {
            _connectLock.Release();
        }
    }

    // Closes channel and connection after a failed publish attempt, so that
    // the next call to GetChannelAsync establishes a fresh connection
    private async Task ResetConnectionAsync()
    {
        try
        {
            if (_channel != null) await _channel.CloseAsync();
            if (_connection != null) await _connection.CloseAsync();
        }
        catch
        {
        }

        _channel = null;
        _connection = null;
    }

    // Cleanly releases channel and connection when the service shuts down
    public async ValueTask DisposeAsync()
    {
        if (_channel != null) await _channel.DisposeAsync();
        if (_connection != null) await _connection.DisposeAsync();
    }
}
