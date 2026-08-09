using FleetControlServer.Api.Grpc;
using FleetControlServer.Api.Realtime;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.AspNetCore.SignalR;

namespace FleetControlServer.Api.Services;

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

        return new StartTripResponse
        {
            TripId = trip.Id.ToString(),
        };
    }

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

        // Best-effort: die Fahrten-Seite soll das Pulsieren des letzten Punkts
        // sofort beenden, statt erst nach einem manuellen Neuladen zu
        // erfahren, dass die Fahrt jetzt abgeschlossen ist.
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
