
using System.Buffers;
using System.Linq;

using MQTTnet;
using System.Text;

public class MqttClientService
{
    private static readonly TimeSpan ReconnectDelay = TimeSpan.FromSeconds(5);

    private readonly IConfiguration _configuration;
    private readonly ILogger<MqttClientService> _logger;
    private readonly MessageHandler _handler;

    private IMqttClient? _client;
    private MqttClientOptions? _options;
    private CancellationToken _stoppingToken;
    private readonly SemaphoreSlim _reconnectLock = new(1, 1);


    public MqttClientService(
        IConfiguration configuration,
        ILogger<MqttClientService> logger,
        MessageHandler handler)
    {
        _configuration = configuration;
        _logger = logger;
        _handler = handler;
    }


    public async Task StartAsync(
        CancellationToken token)
    {
        _logger.LogInformation(
            "MqttClientService StartAsync aufgerufen");

        _stoppingToken = token;

        var factory = new MqttClientFactory();

        _client = factory.CreateMqttClient();


        _client.ApplicationMessageReceivedAsync += async e =>
        {
            var topic = e.ApplicationMessage.Topic;

            var payload =
                Encoding.UTF8.GetString(
                    e.ApplicationMessage.Payload.FirstSpan);


            _logger.LogInformation(
                "MQTT [{topic}]: {msg}",
                topic,
                payload);


            await HandleMessage(
                topic,
                payload);
        };


        _client.DisconnectedAsync += async e =>
        {
            if (_stoppingToken.IsCancellationRequested)
            {
                return;
            }

            if (!await _reconnectLock.WaitAsync(0))
            {
                return;
            }

            try
            {
                _logger.LogError(
                    e.Exception,
                    "MQTT-Verbindung verloren ({reason}), versuche erneut zu verbinden",
                    e.Reason);

                await ConnectWithRetryAsync(
                    _options!,
                    _stoppingToken);

                await _client.SubscribeAsync(
                    _configuration["Mqtt:Topic"]!);

                _logger.LogInformation(
                    "MQTT nach Verbindungsverlust wieder verbunden und Topic neu abonniert");
            }
            finally
            {
                _reconnectLock.Release();
            }
        };


        var optionsBuilder =
            new MqttClientOptionsBuilder()
                .WithTcpServer(
                    _configuration["Mqtt:Host"],
                    int.Parse(
                        _configuration["Mqtt:Port"]!));

        var username = _configuration["Mqtt:Username"];

        if (!string.IsNullOrEmpty(username))
        {
            optionsBuilder.WithCredentials(
                username,
                _configuration["Mqtt:Password"]);
        }

        if (bool.TryParse(_configuration["Mqtt:UseTls"], out var useTls) && useTls)
        {
            optionsBuilder.WithTlsOptions(o => o.UseTls());
        }

        _options = optionsBuilder.Build();


        await _reconnectLock.WaitAsync(token);

        try
        {
            await ConnectWithRetryAsync(
                _options,
                token);
        }
        finally
        {
            _reconnectLock.Release();
        }


        await _client.SubscribeAsync(
            _configuration["Mqtt:Topic"]!);


        _logger.LogInformation(
            "MQTT verbunden");
    }


    private async Task ConnectWithRetryAsync(
        MqttClientOptions options,
        CancellationToken token)
    {
        var attempt = 0;

        while (true)
        {
            attempt++;

            _logger.LogInformation(
                "MQTT-Verbindungsversuch {attempt} zu {host}:{port}",
                attempt,
                _configuration["Mqtt:Host"],
                _configuration["Mqtt:Port"]);

            try
            {
                await _client!.ConnectAsync(
                    options,
                    token);

                _logger.LogInformation(
                    "MQTT-Verbindung erfolgreich nach {attempt} Versuch(en)",
                    attempt);

                return;
            }
            catch (Exception ex) when (!token.IsCancellationRequested)
            {
                _logger.LogError(
                    ex,
                    "MQTT-Verbindungsversuch {attempt} fehlgeschlagen: {message}",
                    attempt,
                    ex.Message);

                _logger.LogInformation(
                    "Erneuter Verbindungsversuch in {delay}s",
                    ReconnectDelay.TotalSeconds);

                await Task.Delay(
                    ReconnectDelay,
                    token);
            }
        }
    }


    private async Task HandleMessage(
        string topic,
        string payload)
    {
        await _handler.HandleAsync(
            topic,
            payload);
    }
}
