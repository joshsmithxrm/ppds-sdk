using FluentAssertions;
using PPDS.Dataverse.Sql.Ast;
using PPDS.Dataverse.Sql.Parsing;
using Xunit;

namespace PPDS.Dataverse.Tests.Sql.Parsing;

[Trait("Category", "TuiUnit")]
public class CaseExpressionParserTests
{
    [Fact]
    public void Parse_SimpleCaseExpression_InSelect()
    {
        // Arrange
        var sql = "SELECT CASE WHEN status = 1 THEN 'Active' ELSE 'Inactive' END AS status_label FROM account";

        // Act
        var result = SqlParser.Parse(sql);

        // Assert
        result.Columns.Should().HaveCount(1);
        var computed = result.Columns[0] as SqlComputedColumn;
        computed.Should().NotBeNull();
        computed!.Alias.Should().Be("status_label");

        var caseExpr = computed.Expression as SqlCaseExpression;
        caseExpr.Should().NotBeNull();
        caseExpr!.WhenClauses.Should().HaveCount(1);

        var when = caseExpr.WhenClauses[0];
        var condition = when.Condition as SqlComparisonCondition;
        condition.Should().NotBeNull();
        condition!.Column.ColumnName.Should().Be("status");
        condition.Operator.Should().Be(SqlComparisonOperator.Equal);
        condition.Value.Value.Should().Be("1");

        var thenResult = when.Result as SqlLiteralExpression;
        thenResult.Should().NotBeNull();
        thenResult!.Value.Value.Should().Be("Active");

        var elseResult = caseExpr.ElseExpression as SqlLiteralExpression;
        elseResult.Should().NotBeNull();
        elseResult!.Value.Value.Should().Be("Inactive");
    }

    [Fact]
    public void Parse_CaseWithMultipleWhens()
    {
        // Arrange
        var sql = @"SELECT CASE
            WHEN status = 0 THEN 'Draft'
            WHEN status = 1 THEN 'Active'
            WHEN status = 2 THEN 'Closed'
            ELSE 'Unknown'
        END AS status_name FROM account";

        // Act
        var result = SqlParser.Parse(sql);

        // Assert
        var computed = result.Columns[0] as SqlComputedColumn;
        computed.Should().NotBeNull();

        var caseExpr = computed!.Expression as SqlCaseExpression;
        caseExpr.Should().NotBeNull();
        caseExpr!.WhenClauses.Should().HaveCount(3);

        // Verify each WHEN clause
        var when0 = caseExpr.WhenClauses[0].Condition as SqlComparisonCondition;
        when0!.Value.Value.Should().Be("0");
        var then0 = caseExpr.WhenClauses[0].Result as SqlLiteralExpression;
        then0!.Value.Value.Should().Be("Draft");

        var when1 = caseExpr.WhenClauses[1].Condition as SqlComparisonCondition;
        when1!.Value.Value.Should().Be("1");
        var then1 = caseExpr.WhenClauses[1].Result as SqlLiteralExpression;
        then1!.Value.Value.Should().Be("Active");

        var when2 = caseExpr.WhenClauses[2].Condition as SqlComparisonCondition;
        when2!.Value.Value.Should().Be("2");
        var then2 = caseExpr.WhenClauses[2].Result as SqlLiteralExpression;
        then2!.Value.Value.Should().Be("Closed");

        var elseExpr = caseExpr.ElseExpression as SqlLiteralExpression;
        elseExpr!.Value.Value.Should().Be("Unknown");
    }

    [Fact]
    public void Parse_CaseWithoutElse()
    {
        // Arrange
        var sql = "SELECT CASE WHEN status = 1 THEN 'Active' END AS label FROM account";

        // Act
        var result = SqlParser.Parse(sql);

        // Assert
        var computed = result.Columns[0] as SqlComputedColumn;
        computed.Should().NotBeNull();

        var caseExpr = computed!.Expression as SqlCaseExpression;
        caseExpr.Should().NotBeNull();
        caseExpr!.WhenClauses.Should().HaveCount(1);
        caseExpr.ElseExpression.Should().BeNull();
    }

    [Fact]
    public void Parse_IifExpression_InSelect()
    {
        // Arrange
        var sql = "SELECT IIF(revenue > 1000000, 'High', 'Low') AS tier FROM account";

        // Act
        var result = SqlParser.Parse(sql);

        // Assert
        result.Columns.Should().HaveCount(1);
        var computed = result.Columns[0] as SqlComputedColumn;
        computed.Should().NotBeNull();
        computed!.Alias.Should().Be("tier");

        var iifExpr = computed.Expression as SqlIifExpression;
        iifExpr.Should().NotBeNull();

        var condition = iifExpr!.Condition as SqlComparisonCondition;
        condition.Should().NotBeNull();
        condition!.Column.ColumnName.Should().Be("revenue");
        condition.Operator.Should().Be(SqlComparisonOperator.GreaterThan);
        condition.Value.Value.Should().Be("1000000");

        var trueVal = iifExpr.TrueValue as SqlLiteralExpression;
        trueVal.Should().NotBeNull();
        trueVal!.Value.Value.Should().Be("High");

        var falseVal = iifExpr.FalseValue as SqlLiteralExpression;
        falseVal.Should().NotBeNull();
        falseVal!.Value.Value.Should().Be("Low");
    }

    [Fact]
    public void Parse_CaseWithNestedConditions()
    {
        // Arrange — WHEN with AND condition
        var sql = "SELECT CASE WHEN status = 1 AND revenue > 500000 THEN 'Hot' ELSE 'Cold' END AS temp FROM account";

        // Act
        var result = SqlParser.Parse(sql);

        // Assert
        var computed = result.Columns[0] as SqlComputedColumn;
        computed.Should().NotBeNull();

        var caseExpr = computed!.Expression as SqlCaseExpression;
        caseExpr.Should().NotBeNull();

        var when = caseExpr!.WhenClauses[0];
        var logical = when.Condition as SqlLogicalCondition;
        logical.Should().NotBeNull();
        logical!.Operator.Should().Be(SqlLogicalOperator.And);
        logical.Conditions.Should().HaveCount(2);
    }

    [Fact]
    public void Parse_CaseAlias_WithAs()
    {
        // Arrange
        var sql = "SELECT CASE WHEN status = 1 THEN 'Active' END AS my_label FROM account";

        // Act
        var result = SqlParser.Parse(sql);

        // Assert
        var computed = result.Columns[0] as SqlComputedColumn;
        computed.Should().NotBeNull();
        computed!.Alias.Should().Be("my_label");
    }

    [Fact]
    public void Parse_CaseAlias_WithoutAs()
    {
        // Arrange — alias without AS keyword
        var sql = "SELECT CASE WHEN status = 1 THEN 'Active' END my_label FROM account";

        // Act
        var result = SqlParser.Parse(sql);

        // Assert
        var computed = result.Columns[0] as SqlComputedColumn;
        computed.Should().NotBeNull();
        computed!.Alias.Should().Be("my_label");
    }

    [Fact]
    public void Parse_CaseWithRegularColumns()
    {
        // Arrange — CASE mixed with regular columns
        var sql = "SELECT name, CASE WHEN status = 1 THEN 'Active' ELSE 'Inactive' END AS status_label, revenue FROM account";

        // Act
        var result = SqlParser.Parse(sql);

        // Assert
        result.Columns.Should().HaveCount(3);

        var col1 = result.Columns[0] as SqlColumnRef;
        col1.Should().NotBeNull();
        col1!.ColumnName.Should().Be("name");

        var col2 = result.Columns[1] as SqlComputedColumn;
        col2.Should().NotBeNull();
        col2!.Alias.Should().Be("status_label");

        var col3 = result.Columns[2] as SqlColumnRef;
        col3.Should().NotBeNull();
        col3!.ColumnName.Should().Be("revenue");
    }

    [Fact]
    public void Parse_IifWithoutAlias()
    {
        // Arrange
        var sql = "SELECT IIF(status = 1, 'Active', 'Inactive') FROM account";

        // Act
        var result = SqlParser.Parse(sql);

        // Assert
        var computed = result.Columns[0] as SqlComputedColumn;
        computed.Should().NotBeNull();
        computed!.Alias.Should().BeNull();
    }

    [Fact]
    public void Parse_CaseWithColumnResult()
    {
        // Arrange — THEN result is a column reference, not a literal
        var sql = "SELECT CASE WHEN status = 1 THEN name ELSE description END AS display FROM account";

        // Act
        var result = SqlParser.Parse(sql);

        // Assert
        var computed = result.Columns[0] as SqlComputedColumn;
        var caseExpr = computed!.Expression as SqlCaseExpression;
        caseExpr.Should().NotBeNull();

        var thenResult = caseExpr!.WhenClauses[0].Result as SqlColumnExpression;
        thenResult.Should().NotBeNull();
        thenResult!.Column.ColumnName.Should().Be("name");

        var elseResult = caseExpr.ElseExpression as SqlColumnExpression;
        elseResult.Should().NotBeNull();
        elseResult!.Column.ColumnName.Should().Be("description");
    }
}
