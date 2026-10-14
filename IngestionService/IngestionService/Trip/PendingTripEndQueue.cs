using System.Collections.Concurrent;

namespace IngestionService.Trip;

// Holds trip ends whose EndTrip call failed until they have been
// delivered successfully
public class PendingTripEndQueue
{
    private readonly ConcurrentDictionary<string, PendingTripEnd> _pending = new();

    // Queues a failed trip end
    public void Enqueue(PendingTripEnd entry)
    {
        _pending[entry.DeviceId] = entry;
    }

    // Removes a trip end from the queue after it has been delivered successfully
    public void Consume(string deviceId)
    {
        _pending.TryRemove(deviceId, out _);
    }

    // Returns all pending trip ends for a retry run
    public IReadOnlyCollection<PendingTripEnd> Snapshot()
    {
        return _pending.Values.ToList();
    }
}
