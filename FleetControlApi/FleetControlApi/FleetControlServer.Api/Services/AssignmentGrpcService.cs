using FleetControlServer.Api.Grpc;
using Grpc.Core;

namespace FleetControlServer.Api.Services;

// gRPC-Dienst, über den TelemetryDataService die aktuellen Zuordnungen von
// Telemetrie-Einheiten zu Fahrzeug, Kennzeichen und Fahrer abfragt
public class AssignmentGrpcService : AssignmentService.AssignmentServiceBase
{
    private readonly FleetControlServer.Service.TelemetryUnitService _telemetryUnitService;

    public AssignmentGrpcService(FleetControlServer.Service.TelemetryUnitService telemetryUnitService)
    {
        _telemetryUnitService = telemetryUnitService;
    }

    // Liefert die aktuellen Zuordnungen aller registrierten Telemetrie-Einheiten
    public override async Task<GetCurrentAssignmentsResponse> GetCurrentAssignments(
        GetCurrentAssignmentsRequest request,
        ServerCallContext context)
    {
        var assignments = await _telemetryUnitService.GetCurrentAssignmentsAsync();

        var response = new GetCurrentAssignmentsResponse();
        response.Assignments.AddRange(assignments.Select(ToProto));

        return response;
    }

    // Wandelt eine TelemetryAssignment in das gRPC-Proto-Objekt um
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
