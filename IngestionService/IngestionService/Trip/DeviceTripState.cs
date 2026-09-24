namespace IngestionService.Trip;

// Zustand des Fahrtzustandsautomaten für ein einzelnes Gerät, gehalten von TripReactor.
public class DeviceTripState
{
    public TripState State { get; set; }

    public DateTime LastTimestamp { get; set; }

    // Zähler für Fahrtbeendigung
    public int StoppedCount { get; set; }

    // Zähler für Fahrtbegin
    public int DrivingCandidateCount { get; set; }

    public DateTime TripStartTimestamp { get; set; }
}
