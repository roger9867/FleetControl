namespace IngestionService.Trip;

public class DeviceTripState
{
    public TripState State { get; set; }

    public DateTime LastTimestamp { get; set; }

    public int StoppedCount { get; set; }

    public int DrivingCandidateCount { get; set; }

    // Zeitpunkt, zu dem die aktuelle Fahrt logisch begonnen hat - unabhängig
    // davon, ob der StartTrip-Call erfolgreich war. Wird bei EndTrip mitgeschickt,
    // damit MainService den Trip notfalls nachträglich anlegen kann.
    public DateTime TripStartTimestamp { get; set; }
}
