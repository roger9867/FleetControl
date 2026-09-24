using System.Threading.Channels;
using IngestionService.Assignments;
using IngestionService.Models;
using IngestionService.Trip;

namespace IngestionService.Influx;

// Ein verarbeiteter Datenpunkt inkl. erkanntem Fahrtzustand und aktueller Zuordnung
public record InfluxPoint(string Topic, TelemetryEvent Data, TripState State, Assignment Assignment);

// Nimmt Punkte entgegen, ohne zu blockieren
// Eigentliches Schreiben nach InfluxDB passiert in InfluxWriteWorker 
// entkoppelt vom MQTT-Verarbeitungspfad
public class InfluxWriter
{
    private readonly Channel<InfluxPoint> _channel =
        Channel.CreateUnbounded<InfluxPoint>();

    public ChannelReader<InfluxPoint> Reader => _channel.Reader;


    // Legt einen Datenpunkt in der Warteschlange ab
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
