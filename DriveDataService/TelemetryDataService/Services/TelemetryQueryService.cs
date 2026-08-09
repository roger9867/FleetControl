using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using InfluxDB.Client;

namespace TelemetryDataService.Services;

// Reads back what IngestionService/InfluxWriteWorker wrote: measurement
// "sensor", tagged by deviceId (== TelemetryUnit.Id), fields lat/lon/
// speedKmh/accelMs2. There is no tripId in InfluxDB, so a trip's points are
// selected purely by deviceId + [start, end) time range.
public class TelemetryQueryService : TelemetryQuery.TelemetryQueryBase
{
    private readonly InfluxDBClient _client;
    private readonly string _bucket;
    private readonly string _org;
    private readonly ILogger<TelemetryQueryService> _logger;

    public TelemetryQueryService(IConfiguration config, ILogger<TelemetryQueryService> logger)
    {
        _client = new InfluxDBClient(
            config["InfluxDB:Url"]!,
            config["InfluxDB:Token"]!);

        _bucket = config["InfluxDB:Bucket"]!;
        _org = config["InfluxDB:Org"]!;
        _logger = logger;
    }

    public override async Task<GetTelemetryPointsResponse> GetTelemetryPoints(
        GetTelemetryPointsRequest request,
        ServerCallContext context)
    {
        var response = new GetTelemetryPointsResponse();

        var start = request.Start.ToDateTime();
        var end = request.End.ToDateTime();

        // deviceId is compared case-insensitively: TelemetryUnitId.ToString() is
        // always lowercase, but the actual devices report their id in whatever
        // case their firmware uses (observed uppercase in practice).
        var flux =
            "import \"strings\" " +
            $"from(bucket: \"{_bucket}\") " +
            $"|> range(start: {start:yyyy-MM-ddTHH:mm:ssZ}, stop: {end:yyyy-MM-ddTHH:mm:ssZ}) " +
            "|> filter(fn: (r) => r._measurement == \"sensor\") " +
            $"|> filter(fn: (r) => strings.toLower(v: r.deviceId) == \"{request.DeviceId.ToLowerInvariant()}\") " +
            "|> filter(fn: (r) => r._field == \"lat\" or r._field == \"lon\" or r._field == \"speedKmh\" or r._field == \"accelMs2\") " +
            "|> pivot(rowKey: [\"_time\"], columnKey: [\"_field\"], valueColumn: \"_value\") " +
            "|> sort(columns: [\"_time\"])";

        try
        {
            var tables = await _client.GetQueryApi().QueryAsync(flux, _org);

            foreach (var table in tables)
            {
                foreach (var record in table.Records)
                {
                    var time = record.GetTime();
                    if (time == null) continue;

                    var point = new TelemetryPoint
                    {
                        Timestamp = Timestamp.FromDateTime(
                            DateTime.SpecifyKind(time.Value.ToDateTimeUtc(), DateTimeKind.Utc)),
                        Lat = ToDouble(record.GetValueByKey("lat")),
                        Lon = ToDouble(record.GetValueByKey("lon")),
                        SpeedKmh = ToDouble(record.GetValueByKey("speedKmh")),
                        AccelMs2 = ToDouble(record.GetValueByKey("accelMs2"))
                    };

                    response.Points.Add(point);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "InfluxDB query failed for device {deviceId} [{start}, {end}]",
                request.DeviceId,
                start,
                end);
        }

        return response;
    }

    public override async Task<GetLatestPointsResponse> GetLatestPoints(
        GetLatestPointsRequest request,
        ServerCallContext context)
    {
        var response = new GetLatestPointsResponse();

        if (request.DeviceIds.Count == 0)
        {
            return response;
        }

        var deviceIdSet = string.Join(
            ", ",
            request.DeviceIds.Select(id => $"\"{id.ToLowerInvariant()}\""));

        // last() aggregates per input table, not per device - grouping by
        // (deviceId, _field) first guarantees exactly one "last" row per
        // field per device, regardless of which other tags (vehicleId,
        // driverId, ...) happened to be attached at write time. Re-grouping
        // by deviceId alone before pivot lets it recombine those fields -
        // which always share the same _time, since InfluxWriteWorker writes
        // lat/lon/speedKmh/accelMs2 as one atomic point - into a single row.
        var flux =
            "import \"strings\" " +
            $"from(bucket: \"{_bucket}\") " +
            "|> range(start: -90d) " +
            "|> filter(fn: (r) => r._measurement == \"sensor\") " +
            $"|> filter(fn: (r) => contains(value: strings.toLower(v: r.deviceId), set: [{deviceIdSet}])) " +
            "|> filter(fn: (r) => r._field == \"lat\" or r._field == \"lon\" or r._field == \"speedKmh\" or r._field == \"accelMs2\") " +
            "|> group(columns: [\"deviceId\", \"_field\"]) " +
            "|> last() " +
            "|> group(columns: [\"deviceId\"]) " +
            "|> pivot(rowKey: [\"_time\"], columnKey: [\"_field\"], valueColumn: \"_value\")";

        try
        {
            var tables = await _client.GetQueryApi().QueryAsync(flux, _org);

            foreach (var table in tables)
            {
                foreach (var record in table.Records)
                {
                    var time = record.GetTime();
                    if (time == null) continue;

                    var deviceId = record.GetValueByKey("deviceId") as string;
                    if (string.IsNullOrEmpty(deviceId)) continue;

                    response.Points.Add(new DeviceLatestPoint
                    {
                        DeviceId = deviceId,
                        Point = new TelemetryPoint
                        {
                            Timestamp = Timestamp.FromDateTime(
                                DateTime.SpecifyKind(time.Value.ToDateTimeUtc(), DateTimeKind.Utc)),
                            Lat = ToDouble(record.GetValueByKey("lat")),
                            Lon = ToDouble(record.GetValueByKey("lon")),
                            SpeedKmh = ToDouble(record.GetValueByKey("speedKmh")),
                            AccelMs2 = ToDouble(record.GetValueByKey("accelMs2"))
                        }
                    });
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "InfluxDB latest-points query failed for {count} device(s)",
                request.DeviceIds.Count);
        }

        return response;
    }

    public override async Task<GetLatestPointsByVehicleResponse> GetLatestPointsByVehicle(
        GetLatestPointsByVehicleRequest request,
        ServerCallContext context)
    {
        var response = new GetLatestPointsByVehicleResponse();

        if (request.VehicleIds.Count == 0)
        {
            return response;
        }

        var vehicleIdSet = string.Join(
            ", ",
            request.VehicleIds.Select(id => $"\"{id}\""));

        var flux =
            $"from(bucket: \"{_bucket}\") " +
            "|> range(start: -90d) " +
            "|> filter(fn: (r) => r._measurement == \"sensor\") " +
            $"|> filter(fn: (r) => contains(value: r.vehicleId, set: [{vehicleIdSet}])) " +
            "|> filter(fn: (r) => r._field == \"lat\" or r._field == \"lon\" or r._field == \"speedKmh\" or r._field == \"accelMs2\") " +
            "|> group(columns: [\"vehicleId\", \"_field\"]) " +
            "|> last() " +
            "|> group(columns: [\"vehicleId\"]) " +
            "|> pivot(rowKey: [\"_time\"], columnKey: [\"_field\"], valueColumn: \"_value\")";

        try
        {
            var tables = await _client.GetQueryApi().QueryAsync(flux, _org);

            foreach (var table in tables)
            {
                foreach (var record in table.Records)
                {
                    var time = record.GetTime();
                    if (time == null) continue;

                    var vehicleId = record.GetValueByKey("vehicleId") as string;
                    if (string.IsNullOrEmpty(vehicleId)) continue;

                    response.Points.Add(new VehicleLatestPoint
                    {
                        VehicleId = vehicleId,
                        Point = new TelemetryPoint
                        {
                            Timestamp = Timestamp.FromDateTime(
                                DateTime.SpecifyKind(time.Value.ToDateTimeUtc(), DateTimeKind.Utc)),
                            Lat = ToDouble(record.GetValueByKey("lat")),
                            Lon = ToDouble(record.GetValueByKey("lon")),
                            SpeedKmh = ToDouble(record.GetValueByKey("speedKmh")),
                            AccelMs2 = ToDouble(record.GetValueByKey("accelMs2"))
                        }
                    });
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "InfluxDB latest-points-by-vehicle query failed for {count} vehicle(s)",
                request.VehicleIds.Count);
        }

        return response;
    }

    private static double ToDouble(object? value)
    {
        return value switch
        {
            null => 0,
            double d => d,
            _ => Convert.ToDouble(value)
        };
    }
}
