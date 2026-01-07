using System.ComponentModel;
using System.Text.Json.Serialization;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Server;
using PPDS.Dataverse.Query;
using PPDS.Dataverse.Sql.Ast;
using PPDS.Dataverse.Sql.Parsing;
using PPDS.Dataverse.Sql.Transpilation;
using PPDS.Mcp.Infrastructure;

namespace PPDS.Mcp.Tools;

/// <summary>
/// MCP tool that executes SQL queries against Dataverse.
/// </summary>
[McpServerToolType]
public sealed class QuerySqlTool
{
    private readonly McpToolContext _context;

    /// <summary>
    /// Initializes a new instance of the <see cref="QuerySqlTool"/> class.
    /// </summary>
    /// <param name="context">The MCP tool context.</param>
    public QuerySqlTool(McpToolContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    /// <summary>
    /// Executes a SQL query against Dataverse.
    /// </summary>
    /// <param name="sql">SQL SELECT statement to execute.</param>
    /// <param name="maxRows">Maximum number of rows to return (default 100).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Query results with columns and records.</returns>
    [McpServerTool(Name = "ppds_query_sql")]
    [Description("Execute a SQL SELECT query against Dataverse. The SQL is transpiled to FetchXML internally. Supports JOINs, WHERE, ORDER BY, TOP, and aggregate functions. Example: SELECT name, revenue FROM account WHERE statecode = 0 ORDER BY revenue DESC")]
    public async Task<QueryResult> ExecuteAsync(
        [Description("SQL SELECT statement (e.g., 'SELECT name, revenue FROM account WHERE statecode = 0')")]
        string sql,
        [Description("Maximum rows to return (default 100, max 5000)")]
        int maxRows = 100,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sql))
        {
            throw new ArgumentException("The 'sql' parameter is required.", nameof(sql));
        }

        // Cap maxRows to prevent runaway queries.
        maxRows = Math.Clamp(maxRows, 1, 5000);

        // Parse and transpile SQL to FetchXML.
        SqlSelectStatement ast;
        try
        {
            var parser = new SqlParser(sql);
            ast = parser.Parse();
        }
        catch (SqlParseException ex)
        {
            throw new InvalidOperationException($"SQL parse error: {ex.Message}", ex);
        }

        // Apply row limit.
        ast = ast.WithTop(maxRows);

        var transpiler = new SqlToFetchXmlTranspiler();
        var fetchXml = transpiler.Transpile(ast);

        // Execute query.
        await using var serviceProvider = await _context.CreateServiceProviderAsync(cancellationToken).ConfigureAwait(false);
        var queryExecutor = serviceProvider.GetRequiredService<IQueryExecutor>();

        var result = await queryExecutor.ExecuteFetchXmlAsync(
            fetchXml,
            pageNumber: null,
            pagingCookie: null,
            includeCount: false,
            cancellationToken).ConfigureAwait(false);

        return MapToResult(result, fetchXml);
    }

    private static QueryResult MapToResult(PPDS.Dataverse.Query.QueryResult result, string fetchXml)
    {
        return new QueryResult
        {
            EntityName = result.EntityLogicalName,
            Columns = result.Columns.Select(c => new QueryColumnInfo
            {
                LogicalName = c.LogicalName,
                Alias = c.Alias,
                DisplayName = c.DisplayName,
                DataType = c.DataType.ToString(),
                LinkedEntityAlias = c.LinkedEntityAlias
            }).ToList(),
            Records = result.Records.Select(r =>
                r.ToDictionary(
                    kvp => kvp.Key,
                    kvp => MapQueryValue(kvp.Value))).ToList(),
            Count = result.Count,
            MoreRecords = result.MoreRecords,
            ExecutedFetchXml = fetchXml,
            ExecutionTimeMs = result.ExecutionTimeMs
        };
    }

    private static object? MapQueryValue(QueryValue? value)
    {
        if (value == null) return null;

        // For lookups, return structured object.
        if (value.LookupEntityId.HasValue)
        {
            return new Dictionary<string, object?>
            {
                ["value"] = value.Value,
                ["formatted"] = value.FormattedValue,
                ["entityType"] = value.LookupEntityType,
                ["entityId"] = value.LookupEntityId
            };
        }

        // For values with formatting, return structured object.
        if (value.FormattedValue != null)
        {
            return new Dictionary<string, object?>
            {
                ["value"] = value.Value,
                ["formatted"] = value.FormattedValue
            };
        }

        // Simple value.
        return value.Value;
    }
}

/// <summary>
/// Result of a query execution.
/// </summary>
public sealed class QueryResult
{
    /// <summary>
    /// Primary entity logical name.
    /// </summary>
    [JsonPropertyName("entityName")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? EntityName { get; set; }

    /// <summary>
    /// Column metadata.
    /// </summary>
    [JsonPropertyName("columns")]
    public List<QueryColumnInfo> Columns { get; set; } = [];

    /// <summary>
    /// Result records.
    /// </summary>
    [JsonPropertyName("records")]
    public List<Dictionary<string, object?>> Records { get; set; } = [];

    /// <summary>
    /// Number of records returned.
    /// </summary>
    [JsonPropertyName("count")]
    public int Count { get; set; }

    /// <summary>
    /// Whether more records are available.
    /// </summary>
    [JsonPropertyName("moreRecords")]
    public bool MoreRecords { get; set; }

    /// <summary>
    /// The FetchXML that was executed (for debugging).
    /// </summary>
    [JsonPropertyName("executedFetchXml")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ExecutedFetchXml { get; set; }

    /// <summary>
    /// Query execution time in milliseconds.
    /// </summary>
    [JsonPropertyName("executionTimeMs")]
    public long ExecutionTimeMs { get; set; }
}

/// <summary>
/// Column information in query results.
/// </summary>
public sealed class QueryColumnInfo
{
    /// <summary>
    /// Attribute logical name.
    /// </summary>
    [JsonPropertyName("logicalName")]
    public string LogicalName { get; set; } = "";

    /// <summary>
    /// Column alias (if specified in query).
    /// </summary>
    [JsonPropertyName("alias")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Alias { get; set; }

    /// <summary>
    /// Human-readable display name.
    /// </summary>
    [JsonPropertyName("displayName")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? DisplayName { get; set; }

    /// <summary>
    /// Data type (String, Integer, Money, DateTime, etc.).
    /// </summary>
    [JsonPropertyName("dataType")]
    public string DataType { get; set; } = "";

    /// <summary>
    /// Linked entity alias (for joined columns).
    /// </summary>
    [JsonPropertyName("linkedEntityAlias")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? LinkedEntityAlias { get; set; }
}
