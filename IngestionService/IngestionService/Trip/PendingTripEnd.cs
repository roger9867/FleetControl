namespace IngestionService.Trip;

// Trip end not yet successfully reported to FleetControlApi, queued in PendingTripEndQueue
public record PendingTripEnd(
    string DeviceId,
    DateTime StartTimestamp,
    DateTime EndTimestamp);
