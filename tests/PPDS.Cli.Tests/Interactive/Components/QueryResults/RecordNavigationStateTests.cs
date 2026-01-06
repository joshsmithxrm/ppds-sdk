using PPDS.Cli.Interactive.Components.QueryResults;
using PPDS.Dataverse.Query;
using Xunit;

namespace PPDS.Cli.Tests.Interactive.Components.QueryResults;

/// <summary>
/// Tests for RecordNavigationState navigation and state management.
/// </summary>
public class RecordNavigationStateTests
{
    private static QueryResult CreateTestResult(
        int recordCount = 3,
        bool moreRecords = false,
        string entityName = "account")
    {
        var columns = new List<QueryColumn>
        {
            new() { LogicalName = $"{entityName}id", DataType = QueryColumnType.Guid },
            new() { LogicalName = "name", DataType = QueryColumnType.String }
        };

        var records = new List<IReadOnlyDictionary<string, QueryValue>>();
        for (int i = 0; i < recordCount; i++)
        {
            var id = Guid.NewGuid();
            records.Add(new Dictionary<string, QueryValue>
            {
                [$"{entityName}id"] = new() { Value = id },
                ["name"] = new() { Value = $"Record {i}" }
            });
        }

        return new QueryResult
        {
            EntityLogicalName = entityName,
            Columns = columns,
            Records = records,
            Count = recordCount,
            MoreRecords = moreRecords,
            PagingCookie = moreRecords ? "cookie123" : null,
            PageNumber = 1,
            ExecutionTimeMs = 100
        };
    }

    [Fact]
    public void Constructor_InitializesFromQueryResult()
    {
        var result = CreateTestResult(recordCount: 5);
        var state = new RecordNavigationState(result);

        Assert.Equal(5, state.TotalLoaded);
        Assert.Equal(0, state.CurrentIndex);
        Assert.Equal("account", state.EntityName);
        Assert.Equal(2, state.Columns.Count);
        Assert.Equal(100, state.ExecutionTimeMs);
    }

    [Fact]
    public void Constructor_WithEnvironmentUrl_SetsProperty()
    {
        var result = CreateTestResult();
        var state = new RecordNavigationState(result, "https://test.crm.dynamics.com");

        Assert.Equal("https://test.crm.dynamics.com", state.EnvironmentUrl);
    }

    [Fact]
    public void MoveNext_AtStart_MovesToNextRecord()
    {
        var result = CreateTestResult(recordCount: 3);
        var state = new RecordNavigationState(result);

        var moved = state.MoveNext();

        Assert.True(moved);
        Assert.Equal(1, state.CurrentIndex);
    }

    [Fact]
    public void MoveNext_AtEnd_ReturnsFalse()
    {
        var result = CreateTestResult(recordCount: 3);
        var state = new RecordNavigationState(result);
        state.MoveNext();
        state.MoveNext();

        var moved = state.MoveNext();

        Assert.False(moved);
        Assert.Equal(2, state.CurrentIndex);
    }

    [Fact]
    public void MovePrevious_AtStart_ReturnsFalse()
    {
        var result = CreateTestResult();
        var state = new RecordNavigationState(result);

        var moved = state.MovePrevious();

        Assert.False(moved);
        Assert.Equal(0, state.CurrentIndex);
    }

    [Fact]
    public void MovePrevious_NotAtStart_MovesToPreviousRecord()
    {
        var result = CreateTestResult(recordCount: 3);
        var state = new RecordNavigationState(result);
        state.MoveNext();

        var moved = state.MovePrevious();

        Assert.True(moved);
        Assert.Equal(0, state.CurrentIndex);
    }

    [Fact]
    public void JumpTo_ValidIndex_Succeeds()
    {
        var result = CreateTestResult(recordCount: 5);
        var state = new RecordNavigationState(result);

        var jumped = state.JumpTo(3);

        Assert.True(jumped);
        Assert.Equal(3, state.CurrentIndex);
    }

    [Fact]
    public void JumpTo_NegativeIndex_ReturnsFalse()
    {
        var result = CreateTestResult();
        var state = new RecordNavigationState(result);

        var jumped = state.JumpTo(-1);

        Assert.False(jumped);
        Assert.Equal(0, state.CurrentIndex);
    }

    [Fact]
    public void JumpTo_IndexBeyondLoaded_ReturnsFalse()
    {
        var result = CreateTestResult(recordCount: 3);
        var state = new RecordNavigationState(result);

        var jumped = state.JumpTo(5);

        Assert.False(jumped);
        Assert.Equal(0, state.CurrentIndex);
    }

    [Fact]
    public void CanMoveNext_NotAtEnd_ReturnsTrue()
    {
        var result = CreateTestResult(recordCount: 3);
        var state = new RecordNavigationState(result);

        Assert.True(state.CanMoveNext);
    }

    [Fact]
    public void CanMoveNext_AtEndWithMoreRecords_ReturnsTrue()
    {
        var result = CreateTestResult(recordCount: 3, moreRecords: true);
        var state = new RecordNavigationState(result);
        state.JumpTo(2);

        Assert.True(state.CanMoveNext);
    }

    [Fact]
    public void CanMoveNext_AtEndNoMoreRecords_ReturnsFalse()
    {
        var result = CreateTestResult(recordCount: 3, moreRecords: false);
        var state = new RecordNavigationState(result);
        state.JumpTo(2);

        Assert.False(state.CanMoveNext);
    }

    [Fact]
    public void CanMovePrevious_AtStart_ReturnsFalse()
    {
        var result = CreateTestResult();
        var state = new RecordNavigationState(result);

        Assert.False(state.CanMovePrevious);
    }

    [Fact]
    public void CanMovePrevious_NotAtStart_ReturnsTrue()
    {
        var result = CreateTestResult();
        var state = new RecordNavigationState(result);
        state.MoveNext();

        Assert.True(state.CanMovePrevious);
    }

    [Fact]
    public void DisplayTotal_NoMoreRecords_ReturnsCount()
    {
        var result = CreateTestResult(recordCount: 5, moreRecords: false);
        var state = new RecordNavigationState(result);

        Assert.Equal("5", state.DisplayTotal);
    }

    [Fact]
    public void DisplayTotal_MoreRecordsAvailable_ReturnsCountWithPlus()
    {
        var result = CreateTestResult(recordCount: 50, moreRecords: true);
        var state = new RecordNavigationState(result);

        Assert.Equal("50+", state.DisplayTotal);
    }

    [Fact]
    public void CurrentRecord_ReturnsCorrectRecord()
    {
        var result = CreateTestResult(recordCount: 3);
        var state = new RecordNavigationState(result);
        state.MoveNext();

        var record = state.CurrentRecord;

        Assert.Equal("Record 1", record["name"].Value);
    }

    [Fact]
    public void AddPage_AppendsRecords()
    {
        var result = CreateTestResult(recordCount: 3, moreRecords: true);
        var state = new RecordNavigationState(result);

        var page2 = CreateTestResult(recordCount: 2, moreRecords: false);
        state.AddPage(page2);

        Assert.Equal(5, state.TotalLoaded);
        Assert.False(state.MoreRecordsAvailable);
    }

    [Fact]
    public void GetNextPageInfo_ReturnsCorrectValues()
    {
        var result = CreateTestResult(recordCount: 3, moreRecords: true);
        var state = new RecordNavigationState(result);

        var (pageNumber, cookie) = state.GetNextPageInfo();

        Assert.Equal(2, pageNumber);
        Assert.Equal("cookie123", cookie);
    }

    [Fact]
    public void NeedsMoreRecords_AtEndWithMoreAvailable_ReturnsTrue()
    {
        var result = CreateTestResult(recordCount: 3, moreRecords: true);
        var state = new RecordNavigationState(result);
        state.JumpTo(2);

        Assert.True(state.NeedsMoreRecords);
    }

    [Fact]
    public void NeedsMoreRecords_NotAtEnd_ReturnsFalse()
    {
        var result = CreateTestResult(recordCount: 3, moreRecords: true);
        var state = new RecordNavigationState(result);

        Assert.False(state.NeedsMoreRecords);
    }

    [Fact]
    public void GetCurrentRecordUrl_WithValidData_ReturnsUrl()
    {
        var id = Guid.NewGuid();
        var result = new QueryResult
        {
            EntityLogicalName = "account",
            Columns = new List<QueryColumn>
            {
                new() { LogicalName = "accountid", DataType = QueryColumnType.Guid }
            },
            Records = new List<IReadOnlyDictionary<string, QueryValue>>
            {
                new Dictionary<string, QueryValue>
                {
                    ["accountid"] = new() { Value = id }
                }
            },
            Count = 1
        };
        var state = new RecordNavigationState(result, "https://test.crm.dynamics.com");

        var url = state.GetCurrentRecordUrl();

        Assert.NotNull(url);
        Assert.Contains("https://test.crm.dynamics.com/main.aspx", url);
        Assert.Contains($"etn=account", url);
        Assert.Contains($"id={id:D}", url);
        Assert.Contains("pagetype=entityrecord", url);
    }

    [Fact]
    public void GetCurrentRecordUrl_WithTrailingSlash_HandlesCorrectly()
    {
        var id = Guid.NewGuid();
        var result = new QueryResult
        {
            EntityLogicalName = "account",
            Columns = new List<QueryColumn>
            {
                new() { LogicalName = "accountid", DataType = QueryColumnType.Guid }
            },
            Records = new List<IReadOnlyDictionary<string, QueryValue>>
            {
                new Dictionary<string, QueryValue>
                {
                    ["accountid"] = new() { Value = id }
                }
            },
            Count = 1
        };
        var state = new RecordNavigationState(result, "https://test.crm.dynamics.com/");

        var url = state.GetCurrentRecordUrl();

        Assert.NotNull(url);
        Assert.DoesNotContain("//main.aspx", url);
    }

    [Fact]
    public void GetCurrentRecordUrl_NoEnvironmentUrl_ReturnsNull()
    {
        var result = CreateTestResult();
        var state = new RecordNavigationState(result);

        Assert.Null(state.GetCurrentRecordUrl());
    }

    [Fact]
    public void GetCurrentRecordUrl_NoPrimaryKey_ReturnsNull()
    {
        var result = new QueryResult
        {
            EntityLogicalName = "account",
            Columns = new List<QueryColumn>
            {
                new() { LogicalName = "name", DataType = QueryColumnType.String }
            },
            Records = new List<IReadOnlyDictionary<string, QueryValue>>
            {
                new Dictionary<string, QueryValue>
                {
                    ["name"] = new() { Value = "Test" }
                }
            },
            Count = 1
        };
        var state = new RecordNavigationState(result, "https://test.crm.dynamics.com");

        Assert.Null(state.GetCurrentRecordUrl());
    }

    [Fact]
    public void CanBuildRecordUrl_WhenUrlCanBeBuilt_ReturnsTrue()
    {
        var id = Guid.NewGuid();
        var result = new QueryResult
        {
            EntityLogicalName = "account",
            Columns = new List<QueryColumn>
            {
                new() { LogicalName = "accountid", DataType = QueryColumnType.Guid }
            },
            Records = new List<IReadOnlyDictionary<string, QueryValue>>
            {
                new Dictionary<string, QueryValue>
                {
                    ["accountid"] = new() { Value = id }
                }
            },
            Count = 1
        };
        var state = new RecordNavigationState(result, "https://test.crm.dynamics.com");

        Assert.True(state.CanBuildRecordUrl);
    }

    [Fact]
    public void CanBuildRecordUrl_WhenUrlCannotBeBuilt_ReturnsFalse()
    {
        var result = CreateTestResult();
        var state = new RecordNavigationState(result);

        Assert.False(state.CanBuildRecordUrl);
    }

    [Fact]
    public void LastAction_DefaultsToNone()
    {
        var result = CreateTestResult();
        var state = new RecordNavigationState(result);

        Assert.Equal(NavigationAction.None, state.LastAction);
    }

    [Fact]
    public void LastAction_CanBeSet()
    {
        var result = CreateTestResult();
        var state = new RecordNavigationState(result);

        state.LastAction = NavigationAction.Next;

        Assert.Equal(NavigationAction.Next, state.LastAction);
    }

    [Fact]
    public void ShowNullValues_DefaultsToFalse()
    {
        var result = CreateTestResult();
        var state = new RecordNavigationState(result);

        Assert.False(state.ShowNullValues);
    }

    [Fact]
    public void ShowNullValues_CanBeToggled()
    {
        var result = CreateTestResult();
        var state = new RecordNavigationState(result);

        state.ShowNullValues = true;

        Assert.True(state.ShowNullValues);
    }
}
