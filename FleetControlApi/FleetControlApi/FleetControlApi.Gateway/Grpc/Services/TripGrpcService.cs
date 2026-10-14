using FleetControlApi.Service.Notifications;
using Grpc.Core;

namespace FleetControlApi.Gateway.Grpc.Services;

// gRPC service through which TelemetryDataService can start and end trips
// Additionally notifies connected clients about the trip status via ITripNotifier
public class TripGrpcService : TripService.TripServiceBase
{
    private readonly FleetControlApi.Service.TripService _tripService;
    private readonly ITripNotifier _tripNotifier;

    public TripGrpcService(
        FleetControlApi.Service.TripService tripService,
        ITripNotifier tripNotifier)
    {
        _tripService = tripService;
        _tripNotifier = tripNotifier;
    }

    // Starts a new trip for a telemetry unit and notifies connected clients
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

        await _tripNotifier.NotifyTripStartedAsync(trip);

        return new StartTripResponse
        {
            TripId = trip.Id.ToString(),
        };
    }

    // Ends the running trip of a telemetry unit and notifies connected clients
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
            await _tripNotifier.NotifyTripEndedAsync(trip);
        }

        return new EndTripResponse
        {
            TripId = trip.Id.ToString(),
        };
    }
}
