using Microsoft.Extensions.Logging;

namespace FleetControlApi.Persistence.Logging;

// Highlighting for important log output
public static class LoggerHighlightExtensions
{
    private const string Red = "[31m";
    private const string Reset = "[0m";

    // Logs an info message in red
    public static void LogHighlighted(
        this ILogger logger,
        string message,
        params object?[] args)
    {
        logger.LogInformation(Red + message + Reset, args);
    }
}
