using IngestionService.Logging;

namespace IngestionService.Trip;

// Tries every 30 seconds to report pending trip ends (EndTrip failed) again
// to FleetControlApi. On success, the entry is consumed from the queue
public class PendingTripEndRetryService : BackgroundService
{
    private static readonly TimeSpan RetryInterval = TimeSpan.FromSeconds(30);

    private readonly PendingTripEndQueue _queue;
    private readonly TripClient _tripClient;
    private readonly ILogger<PendingTripEndRetryService> _logger;

    public PendingTripEndRetryService(
        PendingTripEndQueue queue,
        TripClient tripClient,
        ILogger<PendingTripEndRetryService> logger
    ) {
        _queue = queue;
        _tripClient = tripClient;
        _logger = logger;
    }

    // Runs in the background and triggers a new delivery attempt for pending trip ends every 30 seconds
    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken
    ) {
        using var timer = new PeriodicTimer(RetryInterval);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await RetryPendingTripEndsAsync();
        }
    }

    // Tries again to report EndTrip to FleetControlApi for every pending trip end,
    // removes the entry from the queue as soon as the report succeeded
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
