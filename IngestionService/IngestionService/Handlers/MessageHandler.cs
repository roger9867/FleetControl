using System.Text.Json;
using IngestionService.Assignments;
using IngestionService.Plausibility;
using IngestionService.Influx;
using IngestionService.Models;
using IngestionService.Trip;

// Verarbeitet einen einzelnen eingehenden MQTT-Datenpunkt: parsen, gegen
// Zuordnungs-Cache und Plausibilitätsfilter prüfen, an den Fahrtzustandsautomaten
// weiterreichen und anschließend zur Persistierung übergeben
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


    // Verarbeitungskette für einen eingehenden Datenpunkt: JSON parsen, fehlende
    // DeviceId oder unbekannte Geräte verwerfen, Plausibilität prüfen und den
    // Punkt danach an Zustandsautomat und Persistenz weiterreichen
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


            // Nur für im System bekannte Geräte werden Daten gespeichert
            if (!_assignmentCache.TryGet(data.DeviceId, out var assignment))
            {
                _logger.LogWarning(
                    "Unbekannte DeviceId {deviceId} - Nachricht wird verworfen",
                    data.DeviceId);

                return;
            }


            // Verwirft Punkte mit Positionsfehlern der Telemetrieeinheit,
            //  bevor sie den Zustandsautomaten oder die Datenbank erreichen
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