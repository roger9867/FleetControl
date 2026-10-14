using FleetControlApi.Domain;
using Grpc.Core;

namespace FleetControlApi.Gateway.Grpc.Services;

// gRPC service through which TelemetryDataService queries the current assignments of
// telemetry units to vehicle, license plate and driver
public class AssignmentGrpcService : AssignmentService.AssignmentServiceBase
{
    private readonly FleetControlApi.Service.TelemetryUnitService _telemetryUnitService;

    public AssignmentGrpcService(FleetControlApi.Service.TelemetryUnitService telemetryUnitService)
    {
        _telemetryUnitService = telemetryUnitService;
    }

    // Returns the current assignments of all registered telemetry units
    public override async Task<GetCurrentAssignmentsResponse> GetCurrentAssignments(
        GetCurrentAssignmentsRequest request,
        ServerCallContext context)
    {
        var units = await _telemetryUnitService.GetAllAsync();

        var response = new GetCurrentAssignmentsResponse();
        response.Assignments.AddRange(units.Select(ToProto));

        return response;
    }

    // Converts a telemetry unit and its assigned vehicle into the gRPC proto object
    private static Assignment ToProto(TelemetryUnit unit)
    {
        var proto = new Assignment
        {
            TelemetryUnitId = unit.Id.ToString()
        };

        var vehicle = unit.Vehicle;

        if (vehicle != null) proto.VehicleId = vehicle.VehicleIdentificationNumber;
        if (vehicle?.LicensePlateNumber != null) proto.LicensePlate = vehicle.LicensePlateNumber;
        if (vehicle?.VehicleDriverId != null) proto.DriverId = vehicle.VehicleDriverId.Value.ToString();

        return proto;
    }
}
