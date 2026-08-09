using Grpc.Core;

namespace IngestionService.Assignments;

public class AssignmentUpdateGrpcService : AssignmentUpdateService.AssignmentUpdateServiceBase
{
    private readonly AssignmentCache _cache;

    public AssignmentUpdateGrpcService(AssignmentCache cache)
    {
        _cache = cache;
    }

    public override Task<PushAssignmentUpdateResponse> PushAssignmentUpdate(
        AssignmentUpdate request,
        ServerCallContext context)
    {
        var assignment = new Assignment
        {
            TelemetryUnitId = request.TelemetryUnitId
        };

        if (request.HasVehicleId) assignment.VehicleId = request.VehicleId;
        if (request.HasLicensePlate) assignment.LicensePlate = request.LicensePlate;
        if (request.HasDriverId) assignment.DriverId = request.DriverId;

        _cache.Update(assignment);

        return Task.FromResult(new PushAssignmentUpdateResponse());
    }
}
