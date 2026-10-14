namespace IngestionService.Trip;

// State of the trip state machine for a single device, held by TripReactor.
public class DeviceTripState
{
    public TripState State { get; set; }

    public DateTime LastTimestamp { get; set; }

    // Counter for trip end
    public int StoppedCount { get; set; }

    // Counter for trip start
    public int DrivingCandidateCount { get; set; }

    public DateTime TripStartTimestamp { get; set; }
}
