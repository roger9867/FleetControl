using System.Collections.Concurrent;

namespace IngestionService.Assignments;

// Hält Zuordnungen von Telemetrieeinheit, Fahrer, Fahrzeug
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

    // Lädt beim Start einmalig den kompletten Zuordnungsbestand von FleetControlApi
    // Wiederholung alle 5s bei Misserfolg, bis es gelingt
    public async Task LoadAsync(CancellationToken token = default)
    {
        var attempt = 0;

        while (true)
        {
            attempt++;

            _logger.LogInformation(
                "Lade T-Einheit-Zuordnungen von FleetControlApi (Versuch {attempt})",
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

    // Schlägt die aktuelle Zuordnung einer Geräte-ID nach, ohne einen Server-Call auszulösen
    public bool TryGet(string telemetryUnitId, out Assignment? assignment)
    {
        return _assignments.TryGetValue(telemetryUnitId, out assignment);
    }

    // Übernimmt eine per gRPC-Push von FleetControlApi gemeldete Zuordnungsänderung in den Cache
    public void Update(Assignment assignment)
    {
        _assignments[assignment.TelemetryUnitId] = assignment;

        _logger.LogInformation(
            "T-Einheit-Zuordnung für {telemetryUnitId} aktualisiert (Push von FleetControlService)",
            assignment.TelemetryUnitId);
    }
}
