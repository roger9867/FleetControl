using FleetControlServer.Api.Grpc;
using Grpc.Core;

namespace FleetControlServer.Api.Services;

public class AssignmentGrpcService : AssignmentService.AssignmentServiceBase
{
    private readonly FleetControlServer.Service.TelemetryUnitService _telemetryUnitService;

    public AssignmentGrpcService(FleetControlServer.Service.TelemetryUnitService telemetryUnitService)
    {
        _telemetryUnitService = telemetryUnitService;
    }

    public override async Task<GetCurrentAssignmentsResponse> GetCurrentAssignments(
        GetCurrentAssignmentsRequest request,
        ServerCallContext context)
    {
        var assignments = await _telemetryUnitService.GetCurrentAssignmentsAsync();

        var response = new GetCurrentAssignmentsResponse();
        response.Assignments.AddRange(assignments.Select(ToProto));

        return response;
    }

    // The generated optional-field setters reject null outright (they only
    // support explicit presence via assignment), so absent values must be
    // left unset rather than assigned - there's no way to express that in
    // an object initializer.
    private static Assignment ToProto(FleetControlServer.Data.Repos.TelemetryAssignment a)
    {
        var proto = new Assignment
        {
            TelemetryUnitId = a.TelemetryUnitId.ToString()
        };

        if (a.VehicleId != null) proto.VehicleId = a.VehicleId;
        if (a.LicensePlateNumber != null) proto.LicensePlate = a.LicensePlateNumber;
        if (a.DriverId != null) proto.DriverId = a.DriverId.Value.ToString();

        return proto;
    }
}
