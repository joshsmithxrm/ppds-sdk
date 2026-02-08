using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;

namespace PPDS.Dataverse.Query.Planning.Nodes;

/// <summary>
/// Executes a FetchXML query and yields rows page-by-page.
/// Leaf node in the execution plan tree.
/// </summary>
public sealed class FetchXmlScanNode : IQueryPlanNode
{
    /// <summary>The FetchXML query to execute.</summary>
    public string FetchXml { get; }

    /// <summary>The entity logical name being queried.</summary>
    public string EntityLogicalName { get; }

    /// <summary>If true, automatically fetch all pages. If false, single page only.</summary>
    public bool AutoPage { get; }

    /// <summary>Maximum rows to return, if any.</summary>
    public int? MaxRows { get; }

    /// <inheritdoc />
    public string Description => $"FetchXmlScan: {EntityLogicalName}" +
        (AutoPage ? " (all pages)" : " (single page)") +
        (MaxRows.HasValue ? $" top {MaxRows}" : "");

    /// <inheritdoc />
    public long EstimatedRows => MaxRows ?? -1;

    /// <inheritdoc />
    public IReadOnlyList<IQueryPlanNode> Children => Array.Empty<IQueryPlanNode>();

    public FetchXmlScanNode(string fetchXml, string entityLogicalName, bool autoPage = true, int? maxRows = null)
    {
        FetchXml = fetchXml ?? throw new ArgumentNullException(nameof(fetchXml));
        EntityLogicalName = entityLogicalName ?? throw new ArgumentNullException(nameof(entityLogicalName));
        AutoPage = autoPage;
        MaxRows = maxRows;
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<QueryRow> ExecuteAsync(
        QueryPlanContext context,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var rowCount = 0;
        string? pagingCookie = null;
        var pageNumber = 1;

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var result = await context.QueryExecutor.ExecuteFetchXmlAsync(
                FetchXml,
                pageNumber,
                pagingCookie,
                includeCount: false,
                cancellationToken).ConfigureAwait(false);

            context.Statistics.PagesFetched++;

            foreach (var record in result.Records)
            {
                if (MaxRows.HasValue && rowCount >= MaxRows.Value)
                {
                    yield break;
                }

                yield return QueryRow.FromRecord(record, result.EntityLogicalName);
                rowCount++;
                context.Statistics.RowsRead++;
            }

            if (!AutoPage || !result.MoreRecords)
            {
                yield break;
            }

            pagingCookie = result.PagingCookie;
            pageNumber++;
        }
    }
}
