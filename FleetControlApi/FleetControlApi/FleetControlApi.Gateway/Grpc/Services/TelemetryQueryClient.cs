using DomainTelemetryPoint = FleetControlApi.Domain.TelemetryPoint;
using FleetControlApi.Service.Telemetry;
using Google.Protobuf.WellKnownTypes;

namespace FleetControlApi.Gateway.Grpc.Services;

// gRPC client for TelemetryDataService
public class TelemetryQueryClient : ITelemetryQueryClient
{
    private readonly FleetControlApi.Gateway.Grpc.TelemetryQuery.TelemetryQueryClient _client;

    // The generated gRPC client is created and configured by DI (AddGrpcClient in Program.cs)
    public TelemetryQueryClient(FleetControlApi.Gateway.Grpc.TelemetryQuery.TelemetryQueryClient client)
    {
        _client = client;
    }

    // Returns all telemetry points of a device in the requested time window.
    public async Task<List<DomainTelemetryPoint>> GetTelemetryPointsAsync(
        string deviceId,
        DateTime start,
        DateTime end)
    {
        var response = await _client.GetTelemetryPointsAsync(
            new GetTelemetryPointsRequest
            {
                DeviceId = deviceId,
                Start = ToProtoTimestamp(start),
                End = ToProtoTimestamp(end)
            });

        return response.Points
            .Select(p => new DomainTelemetryPoint(
                p.Timestamp.ToDateTime(),
                p.Lat,
                p.Lon,
                p.SpeedKmh,
                p.AccelMs2))
            .ToList();
    }

    // Returns the latest telemetry point of each of several devices, by device id.
    public async Task<Dictionary<string, DomainTelemetryPoint>> GetLatestPointsAsync(
        IEnumerable<string> deviceIds)
    {
        var request = new GetLatestPointsRequest();
        request.DeviceIds.AddRange(deviceIds);

        if (request.DeviceIds.Count == 0)
        {
            return new Dictionary<string, DomainTelemetryPoint>();
        }

        var response = await _client.GetLatestPointsAsync(request);

        return response.Points.ToDictionary(
            p => p.DeviceId,
            p => new DomainTelemetryPoint(
                p.Point.Timestamp.ToDateTime(),
                p.Point.Lat,
                p.Point.Lon,
                p.Point.SpeedKmh,
                p.Point.AccelMs2),
            StringComparer.OrdinalIgnoreCase);
    }

    // Returns the latest telemetry point of each of several vehicles, by vehicle id.
    public async Task<Dictionary<string, DomainTelemetryPoint>> GetLatestPointsByVehicleAsync(
        IEnumerable<string> vehicleIds)
    {
        var request = new GetLatestPointsByVehicleRequest();
        request.VehicleIds.AddRange(vehicleIds);

        if (request.VehicleIds.Count == 0)
        {
            return new Dictionary<string, DomainTelemetryPoint>();
        }

        var response = await _client.GetLatestPointsByVehicleAsync(request);

        return response.Points.ToDictionary(
            p => p.VehicleId,
            p => new DomainTelemetryPoint(
                p.Point.Timestamp.ToDateTime(),
                p.Point.Lat,
                p.Point.Lon,
                p.Point.SpeedKmh,
                p.Point.AccelMs2,
                string.IsNullOrEmpty(p.Point.DriverId) ? null : p.Point.DriverId,
                string.IsNullOrEmpty(p.Point.DeviceId) ? null : p.Point.DeviceId));
    }

    // Deletes all telemetry points of a device in the given time window.
    public async Task<bool> DeleteTelemetryPointsAsync(
        string deviceId,
        DateTime start,
        DateTime end)
    {
        var response = await _client.DeleteTelemetryPointsAsync(
            new DeleteTelemetryPointsRequest
            {
                DeviceId = deviceId,
                Start = ToProtoTimestamp(start),
                End = ToProtoTimestamp(end)
            });

        return response.Success;
    }

    // Converts a DateTime value into a UTC protobuf Timestamp.
    private static Timestamp ToProtoTimestamp(DateTime timestamp)
    {
        return Timestamp.FromDateTime(
            DateTime.SpecifyKind(timestamp, DateTimeKind.Utc));
    }
}
