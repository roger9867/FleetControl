using InfluxDB.Client;
using InfluxDB.Client.Writes;

namespace IngestionService.Influx;

// Liest aus der InfluxWriter-Queue und schreibt in einem eigenen Hintergrund-
// Loop nach InfluxDB, damit InfluxDB-Latenz nie den MQTT-Verarbeitungspfad
// blockiert.
public class InfluxWriteWorker : BackgroundService
{
    private readonly InfluxWriter _writer;
    private readonly InfluxDBClient _client;
    private readonly WriteApiAsync _writeApiAsync;
    private readonly string _bucket;
    private readonly string _org;
    private readonly ILogger<InfluxWriteWorker> _logger;


    public InfluxWriteWorker(
        InfluxWriter writer,
        IConfiguration config,
        ILogger<InfluxWriteWorker> logger)
    {
        _writer = writer;

        _client = new InfluxDBClient(
            config["InfluxDB:Url"]!,
            config["InfluxDB:Token"]!);

        _writeApiAsync = _client.GetWriteApiAsync();

        _bucket = config["InfluxDB:Bucket"]!;
        _org = config["InfluxDB:Org"]!;

        _logger = logger;
    }


    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        await foreach (var item in _writer.Reader.ReadAllAsync(stoppingToken))
        {
            var point =
                PointData
                .Measurement("sensor")
                .Tag("deviceId", item.Data.DeviceId)
                .Tag("topic", item.Topic)
                .Tag("state", item.State.ToString())
                .Field("lat", item.Data.Latitude)
                .Field("lon", item.Data.Longitude)
                .Field("speedKmh", item.Data.SpeedKmh)
                .Field("accelMs2", item.Data.AccelerationMs2)
                .Timestamp(
                    item.Data.Timestamp,
                    InfluxDB.Client.Api.Domain.WritePrecision.Ns);

            try
            {
                await _writeApiAsync.WritePointAsync(
                    point,
                    _bucket,
                    _org);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "InfluxDB-Write fehlgeschlagen für {deviceId}",
                    item.Data.DeviceId);
            }
        }
    }
}
