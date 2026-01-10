using System.Text.Json.Serialization;

namespace PPDS.Cli.Commands.Query.History;

/// <summary>
/// JSON output model for a list of query history entries.
/// Used by ListCommand for JSON output.
/// </summary>
public sealed class HistoryListOutput
{
    /// <summary>
    /// The list of history entries.
    /// </summary>
    [JsonPropertyName("entries")]
    public List<HistoryEntryOutput> Entries { get; set; } = [];
}

/// <summary>
/// JSON output model for query history entries.
/// Used by GetCommand and ListCommand for JSON output.
/// </summary>
public sealed class HistoryEntryOutput
{
    /// <summary>
    /// The unique identifier for this history entry.
    /// </summary>
    [JsonPropertyName("id")]
    public required string Id { get; set; }

    /// <summary>
    /// The SQL query that was executed.
    /// </summary>
    [JsonPropertyName("sql")]
    public required string Sql { get; set; }

    /// <summary>
    /// When the query was executed.
    /// </summary>
    [JsonPropertyName("executedAt")]
    public required DateTimeOffset ExecutedAt { get; set; }

    /// <summary>
    /// The number of rows returned, if successful.
    /// </summary>
    [JsonPropertyName("rowCount")]
    public int? RowCount { get; set; }

    /// <summary>
    /// The execution time in milliseconds, if available.
    /// </summary>
    [JsonPropertyName("executionTimeMs")]
    public long? ExecutionTimeMs { get; set; }

    /// <summary>
    /// Whether the query executed successfully.
    /// </summary>
    [JsonPropertyName("success")]
    public required bool Success { get; set; }

    /// <summary>
    /// The error message if the query failed.
    /// </summary>
    [JsonPropertyName("errorMessage")]
    public string? ErrorMessage { get; set; }
}
