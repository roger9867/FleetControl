using IngestionService.Logging;

namespace IngestionService.Trip;

// Versucht alle 30 Sekunden, offene Fahrtenden (EndTrip fehlgeschlagen) erneut
// an MainService zu melden. Bei Erfolg wird der Eintrag aus der Queue consumed.
public class PendingTripEndRetryService : BackgroundService
{
    private static readonly TimeSpan RetryInterval = TimeSpan.FromSeconds(30);

    private readonly PendingTripEndQueue _queue;
    private readonly TripClient _tripClient;
    private readonly ILogger<PendingTripEndRetryService> _logger;

    public PendingTripEndRetryService(
        PendingTripEndQueue queue,
        TripClient tripClient,
        ILogger<PendingTripEndRetryService> logger)
    {
        _queue = queue;
        _tripClient = tripClient;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(RetryInterval);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await RetryPendingTripEndsAsync();
        }
    }

    private async Task RetryPendingTripEndsAsync()
    {
        foreach (var pending in _queue.Snapshot())
        {
            try
            {
                await _tripClient.EndTripAsync(
                    pending.DeviceId,
                    pending.EndTimestamp,
                    pending.StartTimestamp);

                _queue.Consume(pending.DeviceId);

                _logger.LogHighlighted(
                    "gRPC EndTrip (Retry) erfolgreich -> deviceId={deviceId}",
                    pending.DeviceId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "EndTrip-Retry weiterhin fehlgeschlagen für {deviceId}",
                    pending.DeviceId);
            }
        }
    }
}
