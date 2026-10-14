using FleetControlApi.Domain;
using FleetControlApi.Service.Notifications;
using Microsoft.AspNetCore.SignalR;

namespace FleetControlApi.Gateway.SignalR;

// ITripNotifier implementation that broadcasts trip status changes to all browser clients via the VehicleHub
public class SignalRTripNotifier : ITripNotifier
{
    private readonly IHubContext<VehicleHub> _hub;

    public SignalRTripNotifier(IHubContext<VehicleHub> hub)
    {
        _hub = hub;
    }

    public Task NotifyTripStartedAsync(Trip trip)
    {
        return _hub.Clients.All.SendAsync(
            "TripStarted",
            new TripStartedMessage(
                trip.Id.ToString(),
                trip.VehicleId,
                trip.TelemetryUnitId.ToString(),
                trip.DriverId,
                trip.StartTimestamp));
    }

    public Task NotifyTripEndedAsync(Trip trip)
    {
        // Only finished trips have an end timestamp to report
        if (!trip.EndTimestamp.HasValue)
        {
            return Task.CompletedTask;
        }

        return _hub.Clients.All.SendAsync(
            "TripEnded",
            new TripEndedMessage(trip.Id.ToString(), trip.VehicleId, trip.EndTimestamp.Value));
    }
}
