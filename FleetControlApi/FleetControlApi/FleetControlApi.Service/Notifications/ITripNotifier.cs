using FleetControlApi.Domain;

namespace FleetControlApi.Service.Notifications;

// Informs connected clients about trip status changes (implemented in Api via SignalR)
public interface ITripNotifier
{
    // Called after a trip has been started
    Task NotifyTripStartedAsync(Trip trip);

    // Called after a trip has been ended
    Task NotifyTripEndedAsync(Trip trip);
}
