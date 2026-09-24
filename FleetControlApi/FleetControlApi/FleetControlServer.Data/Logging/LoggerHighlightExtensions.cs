using Microsoft.Extensions.Logging;

namespace FleetControlServer.Data.Logging;

// Highlighting für wichtige Druckausgaben
public static class LoggerHighlightExtensions
{
    private const string Red = "[31m";
    private const string Reset = "[0m";

    // Loggt eine Info-Nachricht rot
    public static void LogHighlighted(
        this ILogger logger,
        string message,
        params object?[] args)
    {
        logger.LogInformation(Red + message + Reset, args);
    }
}
