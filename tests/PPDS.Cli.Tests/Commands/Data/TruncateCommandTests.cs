using System.CommandLine;
using System.CommandLine.Parsing;
using PPDS.Cli.Commands.Data;
using Xunit;

namespace PPDS.Cli.Tests.Commands.Data;

public class TruncateCommandTests
{
    private readonly Command _command;

    public TruncateCommandTests()
    {
        _command = TruncateCommand.Create();
    }

    #region Command Structure Tests

    [Fact]
    public void Create_ReturnsCommandWithCorrectName()
    {
        Assert.Equal("truncate", _command.Name);
    }

    [Fact]
    public void Create_ReturnsCommandWithDescription()
    {
        Assert.StartsWith("Delete ALL records from an entity", _command.Description);
    }

    [Fact]
    public void Create_HasRequiredEntityOption()
    {
        var option = _command.Options.FirstOrDefault(o => o.Name == "--entity");
        Assert.NotNull(option);
        Assert.True(option.Required);
        Assert.Contains("-e", option.Aliases);
    }

    [Fact]
    public void Create_HasOptionalDryRunOption()
    {
        var option = _command.Options.FirstOrDefault(o => o.Name == "--dry-run");
        Assert.NotNull(option);
        Assert.False(option.Required);
    }

    [Fact]
    public void Create_HasOptionalForceOption()
    {
        var option = _command.Options.FirstOrDefault(o => o.Name == "--force");
        Assert.NotNull(option);
        Assert.False(option.Required);
    }

    [Fact]
    public void Create_HasOptionalBatchSizeOption()
    {
        var option = _command.Options.FirstOrDefault(o => o.Name == "--batch-size");
        Assert.NotNull(option);
        Assert.False(option.Required);
    }

    [Fact]
    public void Create_HasOptionalBypassPluginsOption()
    {
        var option = _command.Options.FirstOrDefault(o => o.Name == "--bypass-plugins");
        Assert.NotNull(option);
        Assert.False(option.Required);
    }

    [Fact]
    public void Create_HasOptionalBypassFlowsOption()
    {
        var option = _command.Options.FirstOrDefault(o => o.Name == "--bypass-flows");
        Assert.NotNull(option);
        Assert.False(option.Required);
    }

    [Fact]
    public void Create_HasOptionalContinueOnErrorOption()
    {
        var option = _command.Options.FirstOrDefault(o => o.Name == "--continue-on-error");
        Assert.NotNull(option);
        Assert.False(option.Required);
    }

    #endregion

    #region Argument Parsing Tests

    [Fact]
    public void Parse_WithRequiredEntityOption_Succeeds()
    {
        var result = _command.Parse("--entity account");
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Parse_WithShortEntityAlias_Succeeds()
    {
        var result = _command.Parse("-e account");
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Parse_MissingEntity_HasError()
    {
        var result = _command.Parse("");
        Assert.NotEmpty(result.Errors);
    }

    [Fact]
    public void Parse_WithDryRun_Succeeds()
    {
        var result = _command.Parse("-e account --dry-run");
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Parse_WithForce_Succeeds()
    {
        var result = _command.Parse("-e account --force");
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Parse_WithValidBatchSize_Succeeds()
    {
        var result = _command.Parse("-e account --batch-size 500");
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Parse_WithBatchSizeTooLow_HasError()
    {
        var result = _command.Parse("-e account --batch-size 0");
        Assert.NotEmpty(result.Errors);
    }

    [Fact]
    public void Parse_WithBatchSizeTooHigh_HasError()
    {
        var result = _command.Parse("-e account --batch-size 1001");
        Assert.NotEmpty(result.Errors);
    }

    [Theory]
    [InlineData("sync")]
    [InlineData("async")]
    [InlineData("all")]
    public void Parse_WithBypassPlugins_ValidValues_Succeeds(string value)
    {
        var result = _command.Parse($"-e account --bypass-plugins {value}");
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Parse_WithBypassPlugins_InvalidValue_HasError()
    {
        var result = _command.Parse("-e account --bypass-plugins invalid");
        Assert.NotEmpty(result.Errors);
    }

    [Fact]
    public void Parse_WithBypassFlows_Succeeds()
    {
        var result = _command.Parse("-e account --bypass-flows");
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Parse_WithContinueOnError_Succeeds()
    {
        var result = _command.Parse("-e account --continue-on-error");
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Parse_WithAllOptions_Succeeds()
    {
        var result = _command.Parse(
            "-e account --dry-run --force --batch-size 500 " +
            "--bypass-plugins all --bypass-flows --continue-on-error");
        Assert.Empty(result.Errors);
    }

    #endregion

    #region Entity Validation Tests

    [Fact]
    public void Parse_WithEmptyEntityValue_HasError()
    {
        // Entity option with empty string should fail validation
        var result = _command.Parse("-e \"\"");
        Assert.NotEmpty(result.Errors);
    }

    [Fact]
    public void Parse_WithEntityContainingSpaces_HasError()
    {
        // Entity names cannot have spaces
        var result = _command.Parse("-e \"my entity\"");
        Assert.NotEmpty(result.Errors);
    }

    [Fact]
    public void Parse_WithValidEntityLogicalName_Succeeds()
    {
        var result = _command.Parse("-e new_customentity");
        Assert.Empty(result.Errors);
    }

    #endregion
}
