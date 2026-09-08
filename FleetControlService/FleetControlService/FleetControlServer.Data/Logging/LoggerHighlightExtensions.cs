using Microsoft.Extensions.Logging;

namespace FleetControlServer.Data.Logging;

// ANSI-Escape-Codes direkt in der Message, unabhängig vom LogLevel-Farbschema
// des Konsolen-Loggers - damit lassen sich einzelne Aufrufe gezielt rot
// hervorheben statt ganze LogLevel-Kategorien.
public static class LoggerHighlightExtensions
{
    private const string Red = "[31m";
    private const string Reset = "[0m";

    public static void LogHighlighted(
        this ILogger logger,
        string message,
        params object?[] args)
    {
        logger.LogInformation(Red + message + Reset, args);
    }
}
