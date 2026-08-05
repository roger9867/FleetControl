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
