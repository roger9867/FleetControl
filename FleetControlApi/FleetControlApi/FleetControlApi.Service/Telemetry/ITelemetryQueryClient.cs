using FleetControlApi.Domain;

namespace FleetControlApi.Service.Telemetry;

// Queries stored telemetry points from TelemetryDataService (implemented in Gateway via gRPC)
public interface ITelemetryQueryClient
{
    // Returns all telemetry points of a device in the requested time window.
    Task<List<TelemetryPoint>> GetTelemetryPointsAsync(string deviceId, DateTime start, DateTime end);

    // Returns the latest telemetry point of each of several devices, by device id.
    Task<Dictionary<string, TelemetryPoint>> GetLatestPointsAsync(IEnumerable<string> deviceIds);

    // Returns the latest telemetry point of each of several vehicles, by vehicle id.
    Task<Dictionary<string, TelemetryPoint>> GetLatestPointsByVehicleAsync(IEnumerable<string> vehicleIds);

    // Deletes all telemetry points of a device in the given time window.
    Task<bool> DeleteTelemetryPointsAsync(string deviceId, DateTime start, DateTime end);
}
