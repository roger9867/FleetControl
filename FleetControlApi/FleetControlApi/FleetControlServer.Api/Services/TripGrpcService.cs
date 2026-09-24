using FleetControlServer.Api.Grpc;
using FleetControlServer.Api.Realtime;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.AspNetCore.SignalR;

namespace FleetControlServer.Api.Services;

// gRPC-Dienst, über den TelemetryDataService Fahrten starten und beenden kann
// Benachrichtigt zusätzlich verbundene Clients per SignalR über den Fahrtstatus
public class TripGrpcService : TripService.TripServiceBase
{
    private readonly FleetControlServer.Service.TripService _tripService;
    private readonly IHubContext<VehicleHub> _hub;

    public TripGrpcService(
        FleetControlServer.Service.TripService tripService,
        IHubContext<VehicleHub> hub)
    {
        _tripService = tripService;
        _hub = hub;
    }

    // Startet eine neue Fahrt für eine Telemetrie-Einheit und informiert verbundene Clients per SignalR
    public override async Task<StartTripResponse> StartTrip(StartTripRequest request, ServerCallContext context)
    {
        if (!Guid.TryParse(request.TelemetryUnitId, out var telemetryUnitId))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, $"'{request.TelemetryUnitId}' is not a valid telemetry unit id."));
        }

        var trip = await _tripService.StartTripAsync(telemetryUnitId, request.StartTimestamp.ToDateTime());

        if (trip == null)
        {
            throw new RpcException(new Status(StatusCode.NotFound, $"TelemetryUnit with id '{telemetryUnitId}' does not exist."));
        }

        await _hub.Clients.All.SendAsync(
            "TripStarted",
            new TripStartedMessage(
                trip.Id.ToString(),
                trip.VehicleId,
                trip.TelemetryUnitId.ToString(),
                trip.DriverId,
                trip.StartTimestamp));

        return new StartTripResponse
        {
            TripId = trip.Id.ToString(),
        };
    }

    // Beendet die laufende Fahrt einer Telemetrie-Einheit und informiert verbundene Clients per SignalR
    public override async Task<EndTripResponse> EndTrip(EndTripRequest request, ServerCallContext context)
    {
        if (!Guid.TryParse(request.TelemetryUnitId, out var telemetryUnitId))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, $"'{request.TelemetryUnitId}' is not a valid telemetry unit id."));
        }

        DateTime? startTimestamp = request.StartTimestamp != null
            ? request.StartTimestamp.ToDateTime()
            : null;

        var trip = await _tripService.EndTripAsync(telemetryUnitId, request.EndTimestamp.ToDateTime(), startTimestamp);

        if (trip == null)
        {
            throw new RpcException(new Status(StatusCode.FailedPrecondition, $"No open trip and no start_timestamp provided, or TelemetryUnit '{telemetryUnitId}' does not exist."));
        }

        if (trip.EndTimestamp.HasValue)
        {
            await _hub.Clients.All.SendAsync(
                "TripEnded",
                new TripEndedMessage(trip.Id.ToString(), trip.VehicleId, trip.EndTimestamp.Value));
        }

        return new EndTripResponse
        {
            TripId = trip.Id.ToString(),
        };
    }
}
