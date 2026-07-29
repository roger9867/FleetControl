using System.Text.Json;
using IngestionService.Models;

public class MessageHandler
{
    private readonly InfluxWriter _writer;
    private readonly ILogger<MessageHandler> _logger;


    public MessageHandler(
        InfluxWriter writer,
        ILogger<MessageHandler> logger)
    {
        _writer = writer;
        _logger = logger;
    }


    public async Task HandleAsync(
        string topic,
        string payload)
    {
        try
        {
            var data =
    JsonSerializer.Deserialize<TelemetryEvent>(
        payload,
        new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        _logger.LogInformation(
    "Parsed: DeviceId={deviceId}, Value={value}, Timestamp={timestamp}",
    data?.DeviceId,
    data?.Value,
    data?.Timestamp);


            if (data == null)
            {
                _logger.LogWarning(
                    "Ungültige Nachricht: {payload}",
                    payload);

                return;
            }


            if (string.IsNullOrEmpty(data.DeviceId))
            {
                _logger.LogWarning(
                    "DeviceId fehlt");

                return;
            }


            _logger.LogInformation(
                "Event von {device}: {value}",
                data.DeviceId,
                data.Value);


            await _writer.WriteAsync(
                topic,
                data);
        }
        catch(Exception ex)
        {
            _logger.LogError(
                ex,
                "Fehler beim Parsen");
        }
    }

    
}