namespace IngestionService.Logging;


// Highlight individual calls in color
public static class LoggerHighlightExtensions
{
    private const string Red = "[31m";
    private const string Reset = "[0m";

    // Logs with highlighting
    public static void LogHighlighted(
        this ILogger logger,
        string message,
        params object?[] args
    ) {
        logger.LogInformation(Red + message + Reset, args);
    }
}
