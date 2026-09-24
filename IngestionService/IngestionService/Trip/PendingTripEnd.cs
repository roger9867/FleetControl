namespace IngestionService.Trip;

// Noch nicht erfolgreich an FleetControlApi gemeldetes Fahrtende, vorgemerkt in PendingTripEndQueue
public record PendingTripEnd(
    string DeviceId,
    DateTime StartTimestamp,
    DateTime EndTimestamp);
