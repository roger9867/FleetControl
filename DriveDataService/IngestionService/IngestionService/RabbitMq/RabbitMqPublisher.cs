using System.Text.Json;
using RabbitMQ.Client;

namespace IngestionService.RabbitMq;

// Best-effort: veröffentlicht Events nach RabbitMQ, blockiert die
// Influx-Schreib-Pipeline dabei aber nie. Ist RabbitMQ nicht erreichbar,
// wird das Event verworfen und der nächste Datenpunkt versucht die
// Verbindung erneut aufzubauen - anders als bei MQTT/gRPC ist RabbitMQ hier
// ein Nebeneffekt (Frontend-Benachrichtigung), kein kritischer Pfad.
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
        ILogger<RabbitMqPublisher> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task PublishAsync(
        string routingKey,
        object payload,
        CancellationToken token = default)
    {
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

    private async Task ResetConnectionAsync()
    {
        try
        {
            if (_channel != null) await _channel.CloseAsync();
            if (_connection != null) await _connection.CloseAsync();
        }
        catch
        {
            // Best-effort Aufräumen — die alte Verbindung ist ohnehin defekt.
        }

        _channel = null;
        _connection = null;
    }

    public async ValueTask DisposeAsync()
    {
        if (_channel != null) await _channel.DisposeAsync();
        if (_connection != null) await _connection.DisposeAsync();
    }
}
