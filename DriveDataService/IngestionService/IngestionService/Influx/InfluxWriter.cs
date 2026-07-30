using InfluxDB.Client;
using InfluxDB.Client.Writes;
using IngestionService.Models;

public class InfluxWriter
{
    private readonly InfluxDBClient _client;

    private readonly string _bucket;
    private readonly string _org;


    public InfluxWriter(
        IConfiguration config)
    {
        _client = new InfluxDBClient(
            config["InfluxDB:Url"]!,
            config["InfluxDB:Token"]!);


        _bucket =
            config["InfluxDB:Bucket"]!;

        _org =
            config["InfluxDB:Org"]!;
    }


    public async Task WriteAsync(
        string topic,
        TelemetryEvent data)
    {
        var point =
            PointData
            .Measurement("sensor")
            .Tag("deviceId", data.DeviceId)
            .Tag("topic", topic)
            .Field("lat", data.Latitude)
            .Field("lon", data.Longitude)
            .Field("speedKmh", data.SpeedKmh)
            .Field("accelMs2", data.AccelerationMs2)
            .Timestamp(
                data.Timestamp,
                InfluxDB.Client.Api.Domain.WritePrecision.Ns);


        await _client
            .GetWriteApiAsync()
            .WritePointAsync(
                point,
                _bucket,
                _org);
    }
}