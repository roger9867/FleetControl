using FleetControlServer.Api.Grpc;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;

namespace FleetControlServer.Api.Services;

public class TripGrpcService : TripService.TripServiceBase
{
    private readonly FleetControlServer.Service.TripService _tripService;

    public TripGrpcService(FleetControlServer.Service.TripService tripService)
    {
        _tripService = tripService;
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

        return new EndTripResponse
        {
            TripId = trip.Id.ToString(),
        };
    }
}
