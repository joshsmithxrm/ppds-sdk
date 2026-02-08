using FluentAssertions;
using PPDS.Dataverse.Sql.Ast;
using PPDS.Dataverse.Sql.Parsing;
using Xunit;

namespace PPDS.Dataverse.Tests.Sql.Parsing;

[Trait("Category", "TuiUnit")]
public class WindowFunctionParserTests
{
    [Fact]
    public void Parse_RowNumber_WithOrderBy()
    {
        var sql = "SELECT name, ROW_NUMBER() OVER (ORDER BY revenue DESC) AS rn FROM account";

        var result = SqlParser.Parse(sql);

        result.Columns.Should().HaveCount(2);

        // First column: name
        var nameCol = result.Columns[0] as SqlColumnRef;
        nameCol.Should().NotBeNull();
        nameCol!.ColumnName.Should().Be("name");

        // Second column: ROW_NUMBER() OVER (ORDER BY revenue DESC) AS rn
        var computed = result.Columns[1] as SqlComputedColumn;
        computed.Should().NotBeNull();
        computed!.Alias.Should().Be("rn");

        var windowExpr = computed.Expression as SqlWindowExpression;
        windowExpr.Should().NotBeNull();
        windowExpr!.FunctionName.Should().Be("ROW_NUMBER");
        windowExpr.Operand.Should().BeNull();
        windowExpr.PartitionBy.Should().BeNull();
        windowExpr.OrderBy.Should().HaveCount(1);
        windowExpr.OrderBy![0].Column.ColumnName.Should().Be("revenue");
        windowExpr.OrderBy[0].Direction.Should().Be(SqlSortDirection.Descending);
    }

    [Fact]
    public void Parse_SumOver_WithPartitionBy()
    {
        var sql = "SELECT name, SUM(revenue) OVER (PARTITION BY industrycode) AS total_revenue FROM account";

        var result = SqlParser.Parse(sql);

        result.Columns.Should().HaveCount(2);

        var computed = result.Columns[1] as SqlComputedColumn;
        computed.Should().NotBeNull();
        computed!.Alias.Should().Be("total_revenue");

        var windowExpr = computed.Expression as SqlWindowExpression;
        windowExpr.Should().NotBeNull();
        windowExpr!.FunctionName.Should().Be("SUM");

        // Operand should be a column reference to "revenue"
        var operand = windowExpr.Operand as SqlColumnExpression;
        operand.Should().NotBeNull();
        operand!.Column.ColumnName.Should().Be("revenue");

        // PARTITION BY industrycode
        windowExpr.PartitionBy.Should().HaveCount(1);
        var partCol = windowExpr.PartitionBy![0] as SqlColumnExpression;
        partCol.Should().NotBeNull();
        partCol!.Column.ColumnName.Should().Be("industrycode");

        // No ORDER BY
        windowExpr.OrderBy.Should().BeNull();
    }

    [Fact]
    public void Parse_Rank_WithPartitionAndOrderBy()
    {
        var sql = "SELECT name, RANK() OVER (PARTITION BY ownerid ORDER BY createdon) AS rnk FROM account";

        var result = SqlParser.Parse(sql);

        result.Columns.Should().HaveCount(2);

        var computed = result.Columns[1] as SqlComputedColumn;
        computed.Should().NotBeNull();
        computed!.Alias.Should().Be("rnk");

        var windowExpr = computed.Expression as SqlWindowExpression;
        windowExpr.Should().NotBeNull();
        windowExpr!.FunctionName.Should().Be("RANK");
        windowExpr.Operand.Should().BeNull();

        windowExpr.PartitionBy.Should().HaveCount(1);
        var partCol = windowExpr.PartitionBy![0] as SqlColumnExpression;
        partCol.Should().NotBeNull();
        partCol!.Column.ColumnName.Should().Be("ownerid");

        windowExpr.OrderBy.Should().HaveCount(1);
        windowExpr.OrderBy![0].Column.ColumnName.Should().Be("createdon");
        windowExpr.OrderBy[0].Direction.Should().Be(SqlSortDirection.Ascending);
    }

    [Fact]
    public void Parse_DenseRank_WithOrderByDesc()
    {
        var sql = "SELECT name, DENSE_RANK() OVER (ORDER BY revenue DESC) AS dr FROM account";

        var result = SqlParser.Parse(sql);

        result.Columns.Should().HaveCount(2);

        var computed = result.Columns[1] as SqlComputedColumn;
        computed.Should().NotBeNull();
        computed!.Alias.Should().Be("dr");

        var windowExpr = computed.Expression as SqlWindowExpression;
        windowExpr.Should().NotBeNull();
        windowExpr!.FunctionName.Should().Be("DENSE_RANK");
        windowExpr.Operand.Should().BeNull();
        windowExpr.PartitionBy.Should().BeNull();
        windowExpr.OrderBy.Should().HaveCount(1);
        windowExpr.OrderBy![0].Column.ColumnName.Should().Be("revenue");
        windowExpr.OrderBy[0].Direction.Should().Be(SqlSortDirection.Descending);
    }

    [Fact]
    public void Parse_CountStarOver_WithPartitionBy()
    {
        var sql = "SELECT name, COUNT(*) OVER (PARTITION BY statecode) AS cnt FROM account";

        var result = SqlParser.Parse(sql);

        result.Columns.Should().HaveCount(2);

        var computed = result.Columns[1] as SqlComputedColumn;
        computed.Should().NotBeNull();
        computed!.Alias.Should().Be("cnt");

        var windowExpr = computed.Expression as SqlWindowExpression;
        windowExpr.Should().NotBeNull();
        windowExpr!.FunctionName.Should().Be("COUNT");
        windowExpr.IsCountStar.Should().BeTrue();
        windowExpr.Operand.Should().BeNull();

        windowExpr.PartitionBy.Should().HaveCount(1);
        var partCol = windowExpr.PartitionBy![0] as SqlColumnExpression;
        partCol.Should().NotBeNull();
        partCol!.Column.ColumnName.Should().Be("statecode");
    }

    [Fact]
    public void Parse_WindowFunction_WithAlias()
    {
        var sql = "SELECT ROW_NUMBER() OVER (ORDER BY name ASC) AS row_num FROM account";

        var result = SqlParser.Parse(sql);

        result.Columns.Should().HaveCount(1);

        var computed = result.Columns[0] as SqlComputedColumn;
        computed.Should().NotBeNull();
        computed!.Alias.Should().Be("row_num");

        var windowExpr = computed.Expression as SqlWindowExpression;
        windowExpr.Should().NotBeNull();
        windowExpr!.FunctionName.Should().Be("ROW_NUMBER");
        windowExpr.OrderBy.Should().HaveCount(1);
        windowExpr.OrderBy![0].Column.ColumnName.Should().Be("name");
        windowExpr.OrderBy[0].Direction.Should().Be(SqlSortDirection.Ascending);
    }

    [Fact]
    public void Parse_AggregateWithoutOver_IsNotWindowFunction()
    {
        // Regular aggregate, not a window function
        var sql = "SELECT COUNT(*) AS cnt FROM account";

        var result = SqlParser.Parse(sql);

        result.Columns.Should().HaveCount(1);

        // Should be SqlAggregateColumn, not SqlComputedColumn with SqlWindowExpression
        var aggCol = result.Columns[0] as SqlAggregateColumn;
        aggCol.Should().NotBeNull();
        aggCol!.Function.Should().Be(SqlAggregateFunction.Count);
    }

    [Fact]
    public void Parse_MultipleWindowFunctions()
    {
        var sql = @"SELECT name,
            ROW_NUMBER() OVER (ORDER BY revenue DESC) AS rn,
            RANK() OVER (ORDER BY revenue DESC) AS rnk,
            DENSE_RANK() OVER (ORDER BY revenue DESC) AS dr
            FROM account";

        var result = SqlParser.Parse(sql);

        result.Columns.Should().HaveCount(4);

        // name
        var nameCol = result.Columns[0] as SqlColumnRef;
        nameCol.Should().NotBeNull();

        // ROW_NUMBER
        var rn = (result.Columns[1] as SqlComputedColumn)?.Expression as SqlWindowExpression;
        rn.Should().NotBeNull();
        rn!.FunctionName.Should().Be("ROW_NUMBER");

        // RANK
        var rnk = (result.Columns[2] as SqlComputedColumn)?.Expression as SqlWindowExpression;
        rnk.Should().NotBeNull();
        rnk!.FunctionName.Should().Be("RANK");

        // DENSE_RANK
        var dr = (result.Columns[3] as SqlComputedColumn)?.Expression as SqlWindowExpression;
        dr.Should().NotBeNull();
        dr!.FunctionName.Should().Be("DENSE_RANK");
    }

    [Fact]
    public void Parse_AvgOver_WithPartitionBy()
    {
        var sql = "SELECT name, AVG(revenue) OVER (PARTITION BY industrycode) AS avg_rev FROM account";

        var result = SqlParser.Parse(sql);

        result.Columns.Should().HaveCount(2);

        var computed = result.Columns[1] as SqlComputedColumn;
        computed.Should().NotBeNull();
        computed!.Alias.Should().Be("avg_rev");

        var windowExpr = computed.Expression as SqlWindowExpression;
        windowExpr.Should().NotBeNull();
        windowExpr!.FunctionName.Should().Be("AVG");

        var operand = windowExpr.Operand as SqlColumnExpression;
        operand.Should().NotBeNull();
        operand!.Column.ColumnName.Should().Be("revenue");

        windowExpr.PartitionBy.Should().HaveCount(1);
    }

    [Fact]
    public void Parse_MinMaxOver_WithPartitionBy()
    {
        var sql = "SELECT MIN(revenue) OVER (PARTITION BY industrycode) AS min_rev, MAX(revenue) OVER (PARTITION BY industrycode) AS max_rev FROM account";

        var result = SqlParser.Parse(sql);

        result.Columns.Should().HaveCount(2);

        var minComputed = result.Columns[0] as SqlComputedColumn;
        minComputed.Should().NotBeNull();
        var minWindow = minComputed!.Expression as SqlWindowExpression;
        minWindow.Should().NotBeNull();
        minWindow!.FunctionName.Should().Be("MIN");

        var maxComputed = result.Columns[1] as SqlComputedColumn;
        maxComputed.Should().NotBeNull();
        var maxWindow = maxComputed!.Expression as SqlWindowExpression;
        maxWindow.Should().NotBeNull();
        maxWindow!.FunctionName.Should().Be("MAX");
    }
}
