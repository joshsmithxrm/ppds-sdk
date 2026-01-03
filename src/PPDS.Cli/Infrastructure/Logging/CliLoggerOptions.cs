using Microsoft.Extensions.Logging;

namespace PPDS.Cli.Infrastructure.Logging;

/// <summary>
/// Configuration options for CLI logging.
/// </summary>
public sealed class CliLoggerOptions
{
    /// <summary>
    /// Gets or sets the minimum log level.
    /// </summary>
    public LogLevel MinimumLevel { get; set; } = LogLevel.Information;

    /// <summary>
    /// Gets or sets whether to use JSON output format.
    /// </summary>
    public bool UseJsonFormat { get; set; }

    /// <summary>
    /// Gets or sets the correlation ID (auto-generated if null).
    /// </summary>
    public string? CorrelationId { get; set; }

    /// <summary>
    /// Gets or sets whether colors are enabled.
    /// Defaults to true; will be disabled if NO_COLOR env var is set or stderr is redirected.
    /// </summary>
    public bool EnableColors { get; set; } = true;

    /// <summary>
    /// Gets or sets the timestamp format for text output.
    /// </summary>
    public string TimestampFormat { get; set; } = "HH:mm:ss.fff";

    /// <summary>
    /// Resolves the log level from verbosity flags.
    /// </summary>
    /// <param name="quiet">Whether --quiet flag was specified.</param>
    /// <param name="verbose">Whether --verbose flag was specified.</param>
    /// <param name="debug">Whether --debug flag was specified.</param>
    /// <returns>The resolved log level.</returns>
    public static LogLevel ResolveLogLevel(bool quiet, bool verbose, bool debug)
    {
        // Priority: debug > verbose > quiet > default
        if (debug) return LogLevel.Trace;
        if (verbose) return LogLevel.Debug;
        if (quiet) return LogLevel.Warning;
        return LogLevel.Information;
    }
}
