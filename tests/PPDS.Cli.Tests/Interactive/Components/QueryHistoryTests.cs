using PPDS.Cli.Interactive.Components;
using Xunit;

namespace PPDS.Cli.Tests.Interactive.Components;

/// <summary>
/// Tests for QueryHistory in-memory query history management.
/// </summary>
public class QueryHistoryTests : IDisposable
{
    public QueryHistoryTests()
    {
        // Clear history before each test
        QueryHistory.Clear();
    }

    public void Dispose()
    {
        // Clean up after each test
        QueryHistory.Clear();
    }

    [Fact]
    public void Add_NewQuery_AppearsInRecent()
    {
        QueryHistory.Add("SELECT * FROM account");

        Assert.Single(QueryHistory.Recent);
        Assert.Equal("SELECT * FROM account", QueryHistory.Recent[0]);
    }

    [Fact]
    public void Add_MultipleQueries_MostRecentFirst()
    {
        QueryHistory.Add("SELECT * FROM account");
        QueryHistory.Add("SELECT * FROM contact");

        Assert.Equal(2, QueryHistory.Recent.Count);
        Assert.Equal("SELECT * FROM contact", QueryHistory.Recent[0]);
        Assert.Equal("SELECT * FROM account", QueryHistory.Recent[1]);
    }

    [Fact]
    public void Add_DuplicateQuery_MovesToFront()
    {
        QueryHistory.Add("SELECT * FROM account");
        QueryHistory.Add("SELECT * FROM contact");
        QueryHistory.Add("SELECT * FROM account");

        Assert.Equal(2, QueryHistory.Recent.Count);
        Assert.Equal("SELECT * FROM account", QueryHistory.Recent[0]);
        Assert.Equal("SELECT * FROM contact", QueryHistory.Recent[1]);
    }

    [Fact]
    public void Add_DuplicateQueryWithDifferentWhitespace_MovesToFront()
    {
        QueryHistory.Add("SELECT * FROM account");
        QueryHistory.Add("SELECT  *  FROM  account");

        // Should be treated as same query (normalized)
        Assert.Single(QueryHistory.Recent);
    }

    [Fact]
    public void Add_DuplicateQueryWithDifferentCase_MovesToFront()
    {
        QueryHistory.Add("SELECT * FROM account");
        QueryHistory.Add("select * from account");

        // Should be treated as same query (case insensitive)
        Assert.Single(QueryHistory.Recent);
    }

    [Fact]
    public void Add_EmptyString_NotAdded()
    {
        QueryHistory.Add("");

        Assert.Empty(QueryHistory.Recent);
    }

    [Fact]
    public void Add_WhitespaceOnly_NotAdded()
    {
        QueryHistory.Add("   ");

        Assert.Empty(QueryHistory.Recent);
    }

    [Fact]
    public void Add_NullString_NotAdded()
    {
        QueryHistory.Add(null!);

        Assert.Empty(QueryHistory.Recent);
    }

    [Fact]
    public void Add_QueryWithLeadingTrailingWhitespace_Trimmed()
    {
        QueryHistory.Add("  SELECT * FROM account  ");

        Assert.Equal("SELECT * FROM account", QueryHistory.Recent[0]);
    }

    [Fact]
    public void Add_ExceedsMaxSize_OldestRemoved()
    {
        // Add 25 queries (max is 20)
        for (int i = 0; i < 25; i++)
        {
            QueryHistory.Add($"SELECT {i} FROM table{i}");
        }

        Assert.Equal(20, QueryHistory.Recent.Count);
        // Most recent should be first
        Assert.Equal("SELECT 24 FROM table24", QueryHistory.Recent[0]);
        // Oldest kept should be query 5 (0-4 were dropped)
        Assert.Equal("SELECT 5 FROM table5", QueryHistory.Recent[19]);
    }

    [Fact]
    public void HasHistory_EmptyHistory_ReturnsFalse()
    {
        Assert.False(QueryHistory.HasHistory);
    }

    [Fact]
    public void HasHistory_WithQueries_ReturnsTrue()
    {
        QueryHistory.Add("SELECT * FROM account");

        Assert.True(QueryHistory.HasHistory);
    }

    [Fact]
    public void Clear_RemovesAllQueries()
    {
        QueryHistory.Add("SELECT * FROM account");
        QueryHistory.Add("SELECT * FROM contact");

        QueryHistory.Clear();

        Assert.Empty(QueryHistory.Recent);
        Assert.False(QueryHistory.HasHistory);
    }
}
