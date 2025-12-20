namespace PPDS.Migration.Cli.Commands;

/// <summary>
/// Shared console output helpers for CLI commands.
/// Supports both human-readable and JSON output formats.
/// </summary>
public static class ConsoleOutput
{
    /// <summary>
    /// Writes a progress message to the console.
    /// </summary>
    /// <param name="phase">The current operation phase.</param>
    /// <param name="message">The progress message.</param>
    /// <param name="json">Whether to output as JSON.</param>
    public static void WriteProgress(string phase, string message, bool json)
    {
        if (json)
        {
            Console.WriteLine($"{{\"phase\":\"{phase}\",\"message\":\"{EscapeJson(message)}\",\"timestamp\":\"{DateTime.UtcNow:O}\"}}");
        }
        else
        {
            Console.WriteLine($"[{phase}] {message}");
        }
    }

    /// <summary>
    /// Writes a completion message to the console.
    /// </summary>
    /// <param name="duration">The operation duration.</param>
    /// <param name="recordsProcessed">Number of records processed.</param>
    /// <param name="errors">Number of errors encountered.</param>
    /// <param name="json">Whether to output as JSON.</param>
    public static void WriteCompletion(TimeSpan duration, int recordsProcessed, int errors, bool json)
    {
        if (json)
        {
            Console.WriteLine($"{{\"phase\":\"complete\",\"duration\":\"{duration}\",\"recordsProcessed\":{recordsProcessed},\"errors\":{errors},\"timestamp\":\"{DateTime.UtcNow:O}\"}}");
        }
    }

    /// <summary>
    /// Writes an error message to the console.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="json">Whether to output as JSON.</param>
    public static void WriteError(string message, bool json)
    {
        if (json)
        {
            Console.Error.WriteLine($"{{\"phase\":\"error\",\"message\":\"{EscapeJson(message)}\",\"timestamp\":\"{DateTime.UtcNow:O}\"}}");
        }
        else
        {
            Console.Error.WriteLine($"Error: {message}");
        }
    }

    /// <summary>
    /// Escapes a string for safe inclusion in JSON output.
    /// </summary>
    /// <param name="value">The string to escape.</param>
    /// <returns>The escaped string.</returns>
    public static string EscapeJson(string value) =>
        value.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "\\r");
}
