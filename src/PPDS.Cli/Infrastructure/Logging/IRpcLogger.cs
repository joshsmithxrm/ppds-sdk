namespace PPDS.Cli.Infrastructure.Logging;

/// <summary>
/// Interface for RPC-based logging in daemon mode.
/// This is a placeholder for future VS Code extension integration.
/// </summary>
/// <remarks>
/// When daemon mode is implemented, this interface will be used to send
/// log entries as JSON-RPC notifications to the VS Code extension.
/// </remarks>
public interface IRpcLogger
{
    /// <summary>
    /// Sends a log entry over the RPC channel.
    /// </summary>
    /// <param name="entry">The structured log entry.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    ValueTask SendLogAsync(RpcLogEntry entry, CancellationToken cancellationToken = default);
}

/// <summary>
/// Structured log entry for RPC transmission.
/// </summary>
public sealed record RpcLogEntry
{
    /// <summary>
    /// Gets the timestamp of the log entry.
    /// </summary>
    public required DateTimeOffset Timestamp { get; init; }

    /// <summary>
    /// Gets the log level as a string (trace, debug, information, warning, error, critical).
    /// </summary>
    public required string Level { get; init; }

    /// <summary>
    /// Gets the logger category name.
    /// </summary>
    public required string Category { get; init; }

    /// <summary>
    /// Gets the formatted log message.
    /// </summary>
    public required string Message { get; init; }

    /// <summary>
    /// Gets the correlation ID for distributed tracing.
    /// </summary>
    public string? CorrelationId { get; init; }

    /// <summary>
    /// Gets the command name that generated this log entry.
    /// </summary>
    public string? CommandName { get; init; }

    /// <summary>
    /// Gets the event ID.
    /// </summary>
    public int EventId { get; init; }

    /// <summary>
    /// Gets additional structured properties.
    /// </summary>
    public Dictionary<string, object?>? Properties { get; init; }

    /// <summary>
    /// Gets the exception message if present.
    /// </summary>
    public string? Exception { get; init; }
}
