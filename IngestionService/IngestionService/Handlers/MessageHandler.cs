using System.Text.Json;
using IngestionService.Assignments;
using IngestionService.Plausibility;
using IngestionService.Influx;
using IngestionService.Models;
using IngestionService.Trip;

// Processes a single incoming MQTT data point: parse, check against the
// assignment cache and plausibility filter, pass it on to the trip state machine
// and then hand it over for persistence
public class MessageHandler
{
    private readonly InfluxWriter _writer;
    private readonly TripReactor _tripReactor;
    private readonly AssignmentCache _assignmentCache;
    private readonly GpsPlausibilityFilter _gpsPlausibilityFilter;
    private readonly ILogger<MessageHandler> _logger;


    public MessageHandler(
        InfluxWriter writer,
        TripReactor tripReactor,
        AssignmentCache assignmentCache,
        GpsPlausibilityFilter gpsPlausibilityFilter,
        ILogger<MessageHandler> logger)
    {
        _writer = writer;
        _tripReactor = tripReactor;
        _assignmentCache = assignmentCache;
        _gpsPlausibilityFilter = gpsPlausibilityFilter;
        _logger = logger;
    }


    // Processing chain for an incoming data point: parse JSON, discard missing
    // DeviceId or unknown devices, check plausibility and then
    // pass the point on to the state machine and persistence
    public async Task HandleAsync(
        string topic,
        string payload
    ) {
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


            // Data is only stored for devices known to the system
            if (!_assignmentCache.TryGet(data.DeviceId, out var assignment))
            {
                _logger.LogWarning(
                    "Unbekannte DeviceId {deviceId} - Nachricht wird verworfen",
                    data.DeviceId);

                return;
            }


            // Discards points with position errors from the telemetry unit
            //  before they reach the state machine or the database
            if (!_gpsPlausibilityFilter.IsPlausible(data.DeviceId, data.Latitude, data.Longitude, data.Timestamp))
            {
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
                state,
                assignment!);
        }
        catch(Exception ex)
        {
            _logger.LogError(
                ex,
                "Fehler beim Parsen");
        }
    }

    
}