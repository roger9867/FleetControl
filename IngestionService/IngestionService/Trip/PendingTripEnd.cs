namespace IngestionService.Trip;

public record PendingTripEnd(
    string DeviceId,
    DateTime StartTimestamp,
    DateTime EndTimestamp);
