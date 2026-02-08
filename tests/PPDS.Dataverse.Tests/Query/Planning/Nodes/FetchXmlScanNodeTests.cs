using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using PPDS.Dataverse.Query;
using PPDS.Dataverse.Query.Execution;
using PPDS.Dataverse.Query.Planning;
using PPDS.Dataverse.Query.Planning.Nodes;
using Xunit;

namespace PPDS.Dataverse.Tests.Query.Planning.Nodes;

[Trait("Category", "PlanUnit")]
public class FetchXmlScanNodeTests
{
    private static QueryPlanContext CreateContext(IQueryExecutor executor)
    {
        return new QueryPlanContext(executor, new ExpressionEvaluator());
    }

    private static QueryResult MakeResult(string entity, int count, bool moreRecords = false, string? pagingCookie = null)
    {
        var records = new List<IReadOnlyDictionary<string, QueryValue>>();
        for (int i = 0; i < count; i++)
        {
            records.Add(new Dictionary<string, QueryValue>
            {
                ["name"] = QueryValue.Simple($"Record {i}")
            });
        }

        return new QueryResult
        {
            EntityLogicalName = entity,
            Columns = new List<QueryColumn> { new() { LogicalName = "name" } },
            Records = records,
            Count = count,
            MoreRecords = moreRecords,
            PagingCookie = pagingCookie,
            PageNumber = 1
        };
    }

    [Fact]
    public async Task SinglePage_YieldsAllRows()
    {
        var mockExecutor = new Mock<IQueryExecutor>();
        mockExecutor
            .Setup(x => x.ExecuteFetchXmlAsync(It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<string?>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(MakeResult("account", 3));

        var node = new FetchXmlScanNode("<fetch><entity name='account'><attribute name='name' /></entity></fetch>", "account");
        var ctx = CreateContext(mockExecutor.Object);

        var rows = new List<QueryRow>();
        await foreach (var row in node.ExecuteAsync(ctx))
        {
            rows.Add(row);
        }

        Assert.Equal(3, rows.Count);
        Assert.All(rows, r => Assert.Equal("account", r.EntityLogicalName));
    }

    [Fact]
    public async Task MultiplePages_FetchesAll()
    {
        var page1 = MakeResult("account", 2, moreRecords: true, pagingCookie: "cookie1");
        var page2 = MakeResult("account", 1, moreRecords: false);

        var callCount = 0;
        var mockExecutor = new Mock<IQueryExecutor>();
        mockExecutor
            .Setup(x => x.ExecuteFetchXmlAsync(It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<string?>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => ++callCount == 1 ? page1 : page2);

        var node = new FetchXmlScanNode("<fetch><entity name='account'><attribute name='name' /></entity></fetch>", "account", autoPage: true);
        var ctx = CreateContext(mockExecutor.Object);

        var rows = new List<QueryRow>();
        await foreach (var row in node.ExecuteAsync(ctx))
        {
            rows.Add(row);
        }

        Assert.Equal(3, rows.Count);
        Assert.Equal(2, ctx.Statistics.PagesFetched);
    }

    [Fact]
    public async Task SinglePageMode_StopsAfterFirstPage()
    {
        var result = MakeResult("account", 2, moreRecords: true);
        var mockExecutor = new Mock<IQueryExecutor>();
        mockExecutor
            .Setup(x => x.ExecuteFetchXmlAsync(It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<string?>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(result);

        var node = new FetchXmlScanNode("<fetch />", "account", autoPage: false);
        var ctx = CreateContext(mockExecutor.Object);

        var rows = new List<QueryRow>();
        await foreach (var row in node.ExecuteAsync(ctx))
        {
            rows.Add(row);
        }

        Assert.Equal(2, rows.Count);
        Assert.Equal(1, ctx.Statistics.PagesFetched);
    }

    [Fact]
    public async Task MaxRows_LimitsOutput()
    {
        var result = MakeResult("account", 10);
        var mockExecutor = new Mock<IQueryExecutor>();
        mockExecutor
            .Setup(x => x.ExecuteFetchXmlAsync(It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<string?>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(result);

        var node = new FetchXmlScanNode("<fetch />", "account", maxRows: 5);
        var ctx = CreateContext(mockExecutor.Object);

        var rows = new List<QueryRow>();
        await foreach (var row in node.ExecuteAsync(ctx))
        {
            rows.Add(row);
        }

        Assert.Equal(5, rows.Count);
    }

    [Fact]
    public async Task Cancellation_StopsIteration()
    {
        var cts = new CancellationTokenSource();
        var result = MakeResult("account", 5, moreRecords: true, pagingCookie: "cookie");

        var mockExecutor = new Mock<IQueryExecutor>();
        mockExecutor
            .Setup(x => x.ExecuteFetchXmlAsync(It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<string?>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(result);

        var node = new FetchXmlScanNode("<fetch />", "account");
        var ctx = CreateContext(mockExecutor.Object);

        var rowCount = 0;
        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
        {
            await foreach (var row in node.ExecuteAsync(ctx, cts.Token))
            {
                rowCount++;
                if (rowCount >= 3)
                {
                    cts.Cancel();
                }
            }
        });
    }

    [Fact]
    public void Description_IncludesEntityName()
    {
        var node = new FetchXmlScanNode("<fetch />", "account");
        Assert.Contains("account", node.Description);
    }
}
