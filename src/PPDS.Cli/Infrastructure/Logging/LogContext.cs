namespace PPDS.Cli.Infrastructure.Logging;

/// <summary>
/// Holds contextual information for log entries.
/// </summary>
/// <remarks>
/// LogContext provides correlation IDs and command context for distributed tracing
/// and log aggregation. It is registered as a singleton in the DI container.
/// </remarks>
public sealed class LogContext
{
    /// <summary>
    /// Gets or sets the correlation ID for distributed tracing.
    /// Auto-generated if not provided via --correlation-id flag.
    /// </summary>
    public string CorrelationId { get; set; } = Guid.NewGuid().ToString("D");

    /// <summary>
    /// Gets or sets the command name currently executing.
    /// </summary>
    public string? CommandName { get; set; }

    /// <summary>
    /// Gets the timestamp when the context was created.
    /// </summary>
    public DateTimeOffset StartedAt { get; } = DateTimeOffset.UtcNow;
}
