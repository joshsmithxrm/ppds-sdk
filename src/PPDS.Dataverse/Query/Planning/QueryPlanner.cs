using System;
using System.Collections.Generic;
using PPDS.Dataverse.Query.Planning.Nodes;
using PPDS.Dataverse.Sql.Ast;
using PPDS.Dataverse.Sql.Parsing;
using PPDS.Dataverse.Sql.Transpilation;

namespace PPDS.Dataverse.Query.Planning;

/// <summary>
/// Builds an execution plan for a parsed SQL statement.
/// Phase 0: produces FetchXmlScanNode (equivalent to current pipeline).
/// Subsequent phases add optimization rules and new node types.
/// </summary>
public sealed class QueryPlanner
{
    private readonly SqlToFetchXmlTranspiler _transpiler;

    public QueryPlanner(SqlToFetchXmlTranspiler? transpiler = null)
    {
        _transpiler = transpiler ?? new SqlToFetchXmlTranspiler();
    }

    /// <summary>
    /// Builds an execution plan for a parsed SQL statement.
    /// </summary>
    /// <param name="statement">The parsed SQL statement.</param>
    /// <param name="options">Planning options (pool capacity, row limits, etc.).</param>
    /// <returns>The root node of the execution plan.</returns>
    /// <exception cref="SqlParseException">If the statement type is not supported.</exception>
    public QueryPlanResult Plan(ISqlStatement statement, QueryPlanOptions? options = null)
    {
        if (statement is not SqlSelectStatement selectStatement)
        {
            throw new SqlParseException("Only SELECT statements are currently supported.");
        }

        return PlanSelect(selectStatement, options ?? new QueryPlanOptions());
    }

    private QueryPlanResult PlanSelect(SqlSelectStatement statement, QueryPlanOptions options)
    {
        // Phase 0: transpile to FetchXML and create a simple scan node
        var transpileResult = _transpiler.TranspileWithVirtualColumns(statement);

        var scanNode = new FetchXmlScanNode(
            transpileResult.FetchXml,
            statement.GetEntityName(),
            autoPage: true,
            maxRows: options.MaxRows ?? statement.Top);

        return new QueryPlanResult
        {
            RootNode = scanNode,
            FetchXml = transpileResult.FetchXml,
            VirtualColumns = transpileResult.VirtualColumns,
            EntityLogicalName = statement.GetEntityName()
        };
    }
}

/// <summary>
/// Result of query planning, including the plan tree and metadata needed for execution.
/// </summary>
public sealed class QueryPlanResult
{
    /// <summary>The root node of the execution plan.</summary>
    public required IQueryPlanNode RootNode { get; init; }

    /// <summary>The generated FetchXML (for backward compatibility with SqlQueryResult).</summary>
    public required string FetchXml { get; init; }

    /// <summary>Virtual columns detected during transpilation.</summary>
    public required IReadOnlyDictionary<string, VirtualColumnInfo> VirtualColumns { get; init; }

    /// <summary>The primary entity logical name.</summary>
    public required string EntityLogicalName { get; init; }
}
