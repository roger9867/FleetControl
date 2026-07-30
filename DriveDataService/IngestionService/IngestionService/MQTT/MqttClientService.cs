
using System.Buffers;
using System.Linq;

using MQTTnet;
using System.Text;

public class MqttClientService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<MqttClientService> _logger;
    private readonly MessageHandler _handler;

    private IMqttClient? _client;


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

        var options = optionsBuilder.Build();


        await _client.ConnectAsync(
            options,
            token);


        await _client.SubscribeAsync(
            _configuration["Mqtt:Topic"]!);


        _logger.LogInformation(
            "MQTT verbunden");
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
