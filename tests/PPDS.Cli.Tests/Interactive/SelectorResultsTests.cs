using PPDS.Cli.Interactive.Selectors;
using Xunit;

namespace PPDS.Cli.Tests.Interactive;

/// <summary>
/// Tests for selector result types.
/// </summary>
public class SelectorResultsTests
{
    #region ProfileSelector.SelectionResult

    [Fact]
    public void ProfileSelectionResult_Changed_DefaultsFalse()
    {
        var result = new ProfileSelector.SelectionResult();
        Assert.False(result.Changed);
    }

    [Fact]
    public void ProfileSelectionResult_CreateNew_DefaultsFalse()
    {
        var result = new ProfileSelector.SelectionResult();
        Assert.False(result.CreateNew);
    }

    [Fact]
    public void ProfileSelectionResult_Cancelled_DefaultsFalse()
    {
        var result = new ProfileSelector.SelectionResult();
        Assert.False(result.Cancelled);
    }

    [Fact]
    public void ProfileSelectionResult_Changed_CanBeSet()
    {
        var result = new ProfileSelector.SelectionResult { Changed = true };
        Assert.True(result.Changed);
    }

    [Fact]
    public void ProfileSelectionResult_CreateNew_CanBeSet()
    {
        var result = new ProfileSelector.SelectionResult { CreateNew = true };
        Assert.True(result.CreateNew);
    }

    [Fact]
    public void ProfileSelectionResult_Cancelled_CanBeSet()
    {
        var result = new ProfileSelector.SelectionResult { Cancelled = true };
        Assert.True(result.Cancelled);
    }

    #endregion

    #region EnvironmentSelector.SelectionResult

    [Fact]
    public void EnvironmentSelectionResult_Changed_DefaultsFalse()
    {
        var result = new EnvironmentSelector.SelectionResult();
        Assert.False(result.Changed);
    }

    [Fact]
    public void EnvironmentSelectionResult_Cancelled_DefaultsFalse()
    {
        var result = new EnvironmentSelector.SelectionResult();
        Assert.False(result.Cancelled);
    }

    [Fact]
    public void EnvironmentSelectionResult_ErrorMessage_DefaultsNull()
    {
        var result = new EnvironmentSelector.SelectionResult();
        Assert.Null(result.ErrorMessage);
    }

    [Fact]
    public void EnvironmentSelectionResult_Changed_CanBeSet()
    {
        var result = new EnvironmentSelector.SelectionResult { Changed = true };
        Assert.True(result.Changed);
    }

    [Fact]
    public void EnvironmentSelectionResult_Cancelled_CanBeSet()
    {
        var result = new EnvironmentSelector.SelectionResult { Cancelled = true };
        Assert.True(result.Cancelled);
    }

    [Fact]
    public void EnvironmentSelectionResult_ErrorMessage_CanBeSet()
    {
        var result = new EnvironmentSelector.SelectionResult { ErrorMessage = "Test error" };
        Assert.Equal("Test error", result.ErrorMessage);
    }

    #endregion
}
