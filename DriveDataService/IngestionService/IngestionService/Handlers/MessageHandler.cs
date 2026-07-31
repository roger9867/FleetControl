using System.Text.Json;
using IngestionService.Influx;
using IngestionService.Models;
using IngestionService.Trip;

public class MessageHandler
{
    private readonly InfluxWriter _writer;
    private readonly TripReactor _tripReactor;
    private readonly ILogger<MessageHandler> _logger;


    public MessageHandler(
        InfluxWriter writer,
        TripReactor tripReactor,
        ILogger<MessageHandler> logger)
    {
        _writer = writer;
        _tripReactor = tripReactor;
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
    "Parsed: DeviceId={deviceId}, Lat={lat}, Lon={lon}, SpeedKmh={speed}, AccelMs2={accel}, Timestamp={timestamp}",
    data?.DeviceId,
    data?.Latitude,
    data?.Longitude,
    data?.SpeedKmh,
    data?.AccelerationMs2,
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
                "Event von {device}: lat={lat}, lon={lon}, speed={speed}km/h, accel={accel}m/s2",
                data.DeviceId,
                data.Latitude,
                data.Longitude,
                data.SpeedKmh,
                data.AccelerationMs2);


            var state =
                await _tripReactor.DispatchAsync(
                    data);

            await _writer.WriteAsync(
                topic,
                data,
                state);
        }
        catch(Exception ex)
        {
            _logger.LogError(
                ex,
                "Fehler beim Parsen");
        }
    }

    
}