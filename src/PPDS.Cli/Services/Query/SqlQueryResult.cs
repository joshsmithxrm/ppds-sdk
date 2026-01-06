using PPDS.Dataverse.Query;

namespace PPDS.Cli.Services.Query;

/// <summary>
/// Result of SQL query execution, combining the original SQL,
/// transpiled FetchXML, and query results.
/// </summary>
public sealed class SqlQueryResult
{
    /// <summary>
    /// The original SQL query that was executed.
    /// </summary>
    public required string OriginalSql { get; init; }

    /// <summary>
    /// The FetchXML that the SQL was transpiled to.
    /// </summary>
    public required string TranspiledFetchXml { get; init; }

    /// <summary>
    /// The query result from Dataverse.
    /// </summary>
    public required QueryResult Result { get; init; }
}
