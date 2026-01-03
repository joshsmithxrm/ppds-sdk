using PPDS.Cli.Infrastructure.Errors;

namespace PPDS.Cli.Infrastructure.Output;

/// <summary>
/// Represents the result of a CLI command execution.
/// </summary>
/// <typeparam name="T">The type of the result data.</typeparam>
/// <remarks>
/// This type provides a consistent structure for command results that works
/// with both interactive (text) and daemon (JSON) modes.
/// </remarks>
public sealed record CommandResult<T>
{
    /// <summary>
    /// Whether the operation succeeded (no errors).
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// The result data when successful, or partial data on partial success.
    /// </summary>
    public T? Data { get; init; }

    /// <summary>
    /// The error when the operation failed.
    /// </summary>
    public StructuredError? Error { get; init; }

    /// <summary>
    /// Individual item results for batch operations.
    /// </summary>
    public IReadOnlyList<ItemResult>? Results { get; init; }

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    /// <param name="data">The result data.</param>
    /// <returns>A successful CommandResult.</returns>
    public static CommandResult<T> Ok(T data) => new()
    {
        Success = true,
        Data = data
    };

    /// <summary>
    /// Creates a failed result.
    /// </summary>
    /// <param name="error">The error that caused the failure.</param>
    /// <returns>A failed CommandResult.</returns>
    public static CommandResult<T> Fail(StructuredError error) => new()
    {
        Success = false,
        Error = error
    };

    /// <summary>
    /// Creates a failed result from an error code and message.
    /// </summary>
    /// <param name="code">The error code.</param>
    /// <param name="message">The error message.</param>
    /// <param name="target">Optional target of the error.</param>
    /// <returns>A failed CommandResult.</returns>
    public static CommandResult<T> Fail(string code, string message, string? target = null) => new()
    {
        Success = false,
        Error = new StructuredError(code, message, null, target)
    };

    /// <summary>
    /// Creates a partial success result for batch operations.
    /// </summary>
    /// <param name="data">The partial result data.</param>
    /// <param name="results">The individual item results.</param>
    /// <returns>A partial success CommandResult.</returns>
    public static CommandResult<T> Partial(T data, IEnumerable<ItemResult> results) => new()
    {
        Success = false,
        Data = data,
        Results = results.ToList()
    };
}
