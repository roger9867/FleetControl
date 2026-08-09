using System.Threading.Channels;
using IngestionService.Assignments;
using IngestionService.Models;
using IngestionService.Trip;

namespace IngestionService.Influx;

public record InfluxPoint(string Topic, TelemetryEvent Data, TripState State, Assignment Assignment);

// Nimmt Punkte entgegen, ohne je zu blockieren - das eigentliche Schreiben nach
// InfluxDB passiert in InfluxWriteWorker, entkoppelt vom MQTT-Verarbeitungspfad.
public class InfluxWriter
{
    private readonly Channel<InfluxPoint> _channel =
        Channel.CreateUnbounded<InfluxPoint>();

    public ChannelReader<InfluxPoint> Reader => _channel.Reader;


    public Task WriteAsync(
        string topic,
        TelemetryEvent data,
        TripState state,
        Assignment assignment)
    {
        _channel.Writer.TryWrite(
            new InfluxPoint(topic, data, state, assignment));

        return Task.CompletedTask;
    }
}
