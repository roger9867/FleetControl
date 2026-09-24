using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using InfluxDB.Client;

namespace TelemetryDataService.Services;

// Liest gespeicherte Datenpunkte:
// Measurement "sensor", getaggt mit deviceId, Felder lat/lon/speedKmh/accelMs2.
// Es gibt keine tripId in InfluxDB, daher werden die Punkte einer Fahrt
// ausschliesslich ueber deviceId, Anfangs- u. Endzeitpunkt bestimmt
public class TelemetryQueryService : TelemetryQuery.TelemetryQueryBase
{
    private readonly InfluxDBClient _client;
    private readonly string _bucket;
    private readonly string _org;

    // Zustandsausgabe
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


    // Ausgabe von Telemetriedatenpunkten
    public override async Task<GetTelemetryPointsResponse> GetTelemetryPoints(
        GetTelemetryPointsRequest request,
        ServerCallContext context
    ) {
        var response = new GetTelemetryPointsResponse();

        var start = request.Start.ToDateTime();
        var end = request.End.ToDateTime();

        // deviceId wird case-insensitiv verglichen
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

    // Den letzten Punkt aller Geraete ausgeben
    public override async Task<GetLatestPointsResponse> GetLatestPoints(
        GetLatestPointsRequest request,
        ServerCallContext context
    ) {
        var response = new GetLatestPointsResponse();

        if (request.DeviceIds.Count == 0)
        {
            return response;
        }

        var deviceIdSet = string.Join(
            ", ",
            request.DeviceIds.Select(id => $"\"{id.ToLowerInvariant()}\""));

        // last() aggregiert pro Input-Tabelle, nicht pro Telemetrieeinheit - vorheriges
        // Gruppieren nach (deviceId, _field) garantiert genau eine "last"-Zeile
        // pro Feld und Geraet, unabhaengig davon, welche anderen Tags
        // (vehicleId, driverId, etc) beim Schreiben gesetzt waren. Erneutes
        // Gruppieren nach deviceId allein erlaubt es, diese
        // Felder wieder zu einer Zeile zusammenzufuehren.
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

    // Wie GetLatestPoints, aber ueber vehicleId statt deviceId
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
            "|> group(columns: [\"vehicleId\", \"deviceId\", \"driverId\", \"_field\"]) " +
            "|> last() " +
            "|> group(columns: [\"vehicleId\", \"deviceId\", \"driverId\"]) " +
            "|> pivot(rowKey: [\"_time\"], columnKey: [\"_field\"], valueColumn: \"_value\") " +
            // Ein Fahrzeug kann in den letzten 90 Tagen mehrere unterschiedliche
            // (deviceId, driverId)-Kombinationen gehabt haben - das erzeugt bis
            // hierher eine Zeile pro Kombination. Nochmal nach vehicleId
            // gruppieren und nur die insgesamt juengste Zeile behalten, sonst
            // gibt es pro Fahrzeug mehrere Treffer.
            "|> group(columns: [\"vehicleId\"]) " +
            "|> sort(columns: [\"_time\"], desc: true) " +
            "|> limit(n: 1)";

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
                            AccelMs2 = ToDouble(record.GetValueByKey("accelMs2")),
                            DriverId = record.GetValueByKey("driverId") as string ?? string.Empty,
                            DeviceId = record.GetValueByKey("deviceId") as string ?? string.Empty
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

    // Alle Punkte innerhalb einer Zeitspanne für ein Gerät löschen
    public override async Task<DeleteTelemetryPointsResponse> DeleteTelemetryPoints(
        DeleteTelemetryPointsRequest request,
        ServerCallContext context
    ) {
        var start = request.Start.ToDateTime();
        var end = request.End.ToDateTime();

        try
        {
            var flux =
                "import \"strings\" " +
                $"from(bucket: \"{_bucket}\") " +
                $"|> range(start: {start:yyyy-MM-ddTHH:mm:ssZ}, stop: {end:yyyy-MM-ddTHH:mm:ssZ}) " +
                "|> filter(fn: (r) => r._measurement == \"sensor\") " +
                $"|> filter(fn: (r) => strings.toLower(v: r.deviceId) == \"{request.DeviceId.ToLowerInvariant()}\") " +
                "|> keep(columns: [\"deviceId\"]) " +
                "|> distinct(column: \"deviceId\")";

            var tables = await _client.GetQueryApi().QueryAsync(flux, _org);

            var deviceIds = tables
                .SelectMany(t => t.Records)
                .Select(r => r.GetValueByKey("deviceId") as string)
                .Where(id => !string.IsNullOrEmpty(id))
                .Distinct()
                .ToList();

            foreach (var deviceId in deviceIds)
            {
                var predicate = $"_measurement=\"sensor\" AND deviceId=\"{deviceId}\"";
                await _client.GetDeleteApi().Delete(start, end, predicate, _bucket, _org);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "InfluxDB delete failed for device {deviceId} [{start}, {end}]",
                request.DeviceId,
                start,
                end);
            return new DeleteTelemetryPointsResponse { Success = false };
        }

        return new DeleteTelemetryPointsResponse { Success = true };
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
