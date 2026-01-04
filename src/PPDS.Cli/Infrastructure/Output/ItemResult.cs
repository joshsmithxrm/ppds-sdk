using PPDS.Cli.Infrastructure.Errors;

namespace PPDS.Cli.Infrastructure.Output;

/// <summary>
/// Represents the result of a single item in a batch operation.
/// </summary>
/// <param name="Name">The name or identifier of the item.</param>
/// <param name="Success">Whether processing this item succeeded.</param>
/// <param name="Data">Optional data produced for this item.</param>
/// <param name="Error">Error details if this item failed.</param>
public sealed record ItemResult(
    string Name,
    bool Success,
    object? Data = null,
    StructuredError? Error = null)
{
    /// <summary>
    /// Creates a successful item result.
    /// </summary>
    /// <param name="name">The name or identifier of the item.</param>
    /// <param name="data">Optional data produced for this item.</param>
    /// <returns>A successful ItemResult.</returns>
    public static ItemResult Ok(string name, object? data = null) =>
        new(name, Success: true, Data: data);

    /// <summary>
    /// Creates a failed item result.
    /// </summary>
    /// <param name="name">The name or identifier of the item.</param>
    /// <param name="error">The error that caused the failure.</param>
    /// <returns>A failed ItemResult.</returns>
    public static ItemResult Fail(string name, StructuredError error) =>
        new(name, Success: false, Error: error);

    /// <summary>
    /// Creates a failed item result from an error code and message.
    /// </summary>
    /// <param name="name">The name or identifier of the item.</param>
    /// <param name="code">The error code.</param>
    /// <param name="message">The error message.</param>
    /// <returns>A failed ItemResult.</returns>
    public static ItemResult Fail(string name, string code, string message) =>
        new(name, Success: false, Error: new StructuredError(code, message));
}
