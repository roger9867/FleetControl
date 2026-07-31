using System.Collections.Concurrent;

namespace IngestionService.Trip;

// Hält Fahrtenden, deren EndTrip-Call fehlgeschlagen ist, bis sie erfolgreich
// nachgeliefert (consumed) wurden. Pro Gerät wird nur der jeweils letzte
// offene Endversuch vorgehalten.
public class PendingTripEndQueue
{
    private readonly ConcurrentDictionary<string, PendingTripEnd> _pending = new();

    public void Enqueue(PendingTripEnd entry)
    {
        _pending[entry.DeviceId] = entry;
    }

    public void Consume(string deviceId)
    {
        _pending.TryRemove(deviceId, out _);
    }

    public IReadOnlyCollection<PendingTripEnd> Snapshot()
    {
        return _pending.Values.ToList();
    }
}
