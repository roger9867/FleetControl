using System.Collections.Concurrent;

namespace IngestionService.Plausibility;

// Notlösung für ein Hardware-Problem der Telemetrieeinheit: Durch Timingfehler
// beim Einlesen der GNSS-Antwort werden gelegentlich führende Bytes der
// Koordinaten überschrieben bzw. auf 0 gesetzt, wodurch sporadisch weit
// entfernte Fantasie-Positionen ankommen. Da die eigentliche Fehlerursache in
// der Telemetrieeinheit zeitlich nicht behoben werden konnte, wird  anhand
// der aus zwei Punkten und der verstrichenen Zeit (Haversine-Formel)
// errechneten impliziten Geschwindigkeit geprüft, ob ein Punkt überhaupt
// realistisch erreichbar war.
public class GpsPlausibilityFilter
{
    // Deutlich über realistischer Straßengeschwindigkeit, damit echte,
    // schnelle Fahrten nicht fälschlich verworfen werden.
    private const double MaxPlausibleSpeedKmh = 300;

    // Verhindert, dass ein einmal fehlerhafter Referenzpunkt dauerhaft alle
    // folgenden (korrekten) Punkte blockiert: Nach drei Ablehnungen in Folge
    // für dasselbe Gerät wird der nächste Punkt unabhängig vom Ergebnis als
    // neue Referenz akzeptiert
    private const int MaxConsecutiveRejections = 3;
    private static readonly TimeSpan MaxGapForCheck = TimeSpan.FromMinutes(5);

    private readonly ILogger<GpsPlausibilityFilter> _logger;

    private readonly ConcurrentDictionary<string, (double Lat, double Lon, DateTime Timestamp)> _lastGoodPosition =
        new(StringComparer.OrdinalIgnoreCase);

    private readonly ConcurrentDictionary<string, int> _consecutiveRejections =
        new(StringComparer.OrdinalIgnoreCase);

    public GpsPlausibilityFilter(ILogger<GpsPlausibilityFilter> logger)
    {
        _logger = logger;
    }

    // Prüft, ob eine neue Position anhand der seit dem letzten akzeptierten Punkt
    // verstrichenen Zeit überhaupt erreichbar gewesen wäre, und aktualisiert bei
    // Erfolg (oder erzwungener Übernahme) die Referenzposition des Geräts.
    public bool IsPlausible(string deviceId, double lat, double lon, DateTime timestamp)
    {
        if (!_lastGoodPosition.TryGetValue(deviceId, out var last))
        {
            Accept(deviceId, lat, lon, timestamp);
            return true;
        }

        var elapsed = timestamp - last.Timestamp;

        // Nach einer längeren Pause ist eine
        // beliebig weit entfernte Position plausibel, da echte Zeit zum
        // Zurücklegen der Strecke verstrichen sein kann
        if (elapsed <= TimeSpan.Zero || elapsed > MaxGapForCheck)
        {
            Accept(deviceId, lat, lon, timestamp);
            return true;
        }

        var distanceKm = HaversineDistanceKm(last.Lat, last.Lon, lat, lon);
        var impliedSpeedKmh = distanceKm / elapsed.TotalHours;

        if (impliedSpeedKmh > MaxPlausibleSpeedKmh)
        {
            var rejections = _consecutiveRejections.AddOrUpdate(deviceId, 1, (_, count) => count + 1);

            if (rejections >= MaxConsecutiveRejections)
            {
                _logger.LogWarning(
                    "{count} Punkte fuer {deviceId} in Folge verworfen - naechster Punkt wird als neue Referenz uebernommen",
                    rejections,
                    deviceId);

                Accept(deviceId, lat, lon, timestamp);
                return true;
            }

            _logger.LogWarning(
                "Unplausibler Punkt fuer {deviceId} verworfen: {distance:F1} km in {elapsed:F1}s " +
                "(={speed:F0} km/h) seit letzter Position ({lastLat}, {lastLon}) -> ({lat}, {lon})",
                deviceId,
                distanceKm,
                elapsed.TotalSeconds,
                impliedSpeedKmh,
                last.Lat,
                last.Lon,
                lat,
                lon);

            return false;
        }

        Accept(deviceId, lat, lon, timestamp);
        return true;
    }

    // Übernimmt eine Position als neue Referenz für künftige Plausibilitätsprüfungen
    // und setzt den Ablehnungszähler des Geräts zurück
    private void Accept(string deviceId, double lat, double lon, DateTime timestamp)
    {
        _lastGoodPosition[deviceId] = (lat, lon, timestamp);
        _consecutiveRejections.TryRemove(deviceId, out _);
    }

    // Berechnet die Distanz zwischen zwei Koordinaten in Kilometern über die
    // Haversine-Formel
    private static double HaversineDistanceKm(double lat1, double lon1, double lat2, double lon2)
    {
        const double earthRadiusKm = 6371;

        var dLat = DegreesToRadians(lat2 - lat1);
        var dLon = DegreesToRadians(lon2 - lon1);

        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
            + Math.Cos(DegreesToRadians(lat1)) * Math.Cos(DegreesToRadians(lat2))
            * Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

        return earthRadiusKm * c;
    }

    // Hilfsfunktion für Haversine-Formel die mit Bogenmaß statt Grad rechnet
    private static double DegreesToRadians(double degrees) => degrees * Math.PI / 180;
}
