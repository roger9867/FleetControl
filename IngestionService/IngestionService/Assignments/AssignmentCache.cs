using System.Collections.Concurrent;

namespace IngestionService.Assignments;

// Holds assignments of telemetry unit, driver, vehicle
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

    // Loads the complete set of assignments from FleetControlApi once at startup
    // Retries every 5s on failure until it succeeds
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

    // Looks up the current assignment of a device ID without triggering a server call
    public bool TryGet(string telemetryUnitId, out Assignment? assignment)
    {
        return _assignments.TryGetValue(telemetryUnitId, out assignment);
    }

    // Applies an assignment change reported by FleetControlApi via gRPC push to the cache
    public void Update(Assignment assignment)
    {
        _assignments[assignment.TelemetryUnitId] = assignment;

        _logger.LogInformation(
            "T-Einheit-Zuordnung für {telemetryUnitId} aktualisiert (Push von FleetControlService)",
            assignment.TelemetryUnitId);
    }
}
