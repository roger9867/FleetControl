using System.Collections.Concurrent;

namespace IngestionService.Trip;

// Hält Fahrtenden, deren EndTrip-Call fehlgeschlagen ist, bis sie erfolgreich
// nachgeliefert wurden
public class PendingTripEndQueue
{
    private readonly ConcurrentDictionary<string, PendingTripEnd> _pending = new();

    // Merkt ein fehlgeschlagenes Fahrtende vor
    public void Enqueue(PendingTripEnd entry)
    {
        _pending[entry.DeviceId] = entry;
    }

    // Entfernt ein Fahrtende aus der Warteliste, nachdem es erfolgreich nachgereicht wurde
    public void Consume(string deviceId)
    {
        _pending.TryRemove(deviceId, out _);
    }

    // Liefert alle offenen Fahrtenden für einen Retry-Durchlauf
    public IReadOnlyCollection<PendingTripEnd> Snapshot()
    {
        return _pending.Values.ToList();
    }
}
