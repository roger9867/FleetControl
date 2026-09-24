namespace IngestionService.Logging;


// Einzelne Aufrufe farbig hervorheben
public static class LoggerHighlightExtensions
{
    private const string Red = "[31m";
    private const string Reset = "[0m";

    // Loggt mit Highlighting
    public static void LogHighlighted(
        this ILogger logger,
        string message,
        params object?[] args
    ) {
        logger.LogInformation(Red + message + Reset, args);
    }
}
