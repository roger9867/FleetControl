using System.Collections.Concurrent;

namespace IngestionService.Assignments;

public class GpsPlausibilityFilter
{
    private const double MaxPlausibleSpeedKmh = 300;
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

    public bool IsPlausible(string deviceId, double lat, double lon, DateTime timestamp)
    {
        if (!_lastGoodPosition.TryGetValue(deviceId, out var last))
        {
            Accept(deviceId, lat, lon, timestamp);
            return true;
        }

        var elapsed = timestamp - last.Timestamp;

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

    private void Accept(string deviceId, double lat, double lon, DateTime timestamp)
    {
        _lastGoodPosition[deviceId] = (lat, lon, timestamp);
        _consecutiveRejections.TryRemove(deviceId, out _);
    }

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

    private static double DegreesToRadians(double degrees) => degrees * Math.PI / 180;
}
