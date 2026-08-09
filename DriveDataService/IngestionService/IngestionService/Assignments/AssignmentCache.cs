using System.Collections.Concurrent;

namespace IngestionService.Assignments;

// Hält die beim Start per gRPC geladenen T-Einheit<->Fahrzeug/Fahrer-Tupel im
// Speicher, damit jeder eingehende MQTT-Datenpunkt ohne erneuten Server-Call
// nachschlagen kann, ob und womit seine Geräte-ID aktuell verknüpft ist.
// ConcurrentDictionary, weil TryGet (pro MQTT-Nachricht) und Update (per
// gRPC-Push von MainService, jederzeit) nebenläufig auftreten können.
public class AssignmentCache
{
    private static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(5);

    private readonly AssignmentClient _client;
    private readonly ILogger<AssignmentCache> _logger;

    private ConcurrentDictionary<string, Assignment> _assignments =
        new(StringComparer.OrdinalIgnoreCase);

    public AssignmentCache(
        AssignmentClient client,
        ILogger<AssignmentCache> logger)
    {
        _client = client;
        _logger = logger;
    }

    public async Task LoadAsync(CancellationToken token = default)
    {
        var attempt = 0;

        while (true)
        {
            attempt++;

            _logger.LogInformation(
                "Lade T-Einheit-Zuordnungen von MainService (Versuch {attempt})",
                attempt);

            try
            {
                var assignments = await _client.GetCurrentAssignmentsAsync();

                _assignments = new ConcurrentDictionary<string, Assignment>(
                    assignments.ToDictionary(
                        a => a.TelemetryUnitId,
                        a => a,
                        StringComparer.OrdinalIgnoreCase),
                    StringComparer.OrdinalIgnoreCase);

                _logger.LogInformation(
                    "{count} T-Einheit-Zuordnung(en) geladen (Versuch {attempt})",
                    _assignments.Count,
                    attempt);

                return;
            }
            catch (Exception ex) when (!token.IsCancellationRequested)
            {
                _logger.LogError(
                    ex,
                    "Laden der T-Einheit-Zuordnungen fehlgeschlagen (Versuch {attempt}): {message}",
                    attempt,
                    ex.Message);

                _logger.LogInformation(
                    "Erneuter Versuch in {delay}s",
                    RetryDelay.TotalSeconds);

                await Task.Delay(RetryDelay, token);
            }
        }
    }

    public bool TryGet(string telemetryUnitId, out Assignment? assignment)
    {
        return _assignments.TryGetValue(telemetryUnitId, out assignment);
    }

    public void Update(Assignment assignment)
    {
        _assignments[assignment.TelemetryUnitId] = assignment;

        _logger.LogInformation(
            "T-Einheit-Zuordnung für {telemetryUnitId} aktualisiert (Push von MainService)",
            assignment.TelemetryUnitId);
    }
}
