using System.Threading.Channels;
using IngestionService.Assignments;
using IngestionService.Models;
using IngestionService.Trip;

namespace IngestionService.Influx;

// A processed data point including the detected trip state and current assignment
public record InfluxPoint(string Topic, TelemetryEvent Data, TripState State, Assignment Assignment);

// Accepts points without blocking
// The actual writing to InfluxDB happens in InfluxWriteWorker 
// decoupled from the MQTT processing path
public class InfluxWriter
{
    private readonly Channel<InfluxPoint> _channel =
        Channel.CreateUnbounded<InfluxPoint>();

    public ChannelReader<InfluxPoint> Reader => _channel.Reader;


    // Puts a data point into the queue
    public Task WriteAsync(
        string topic,
        TelemetryEvent data,
        TripState state,
        Assignment assignment
    ) {
        _channel.Writer.TryWrite(
            new InfluxPoint(topic, data, state, assignment));

        return Task.CompletedTask;
    }
}
