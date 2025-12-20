using System.CommandLine;
using System.CommandLine.Parsing;
using PPDS.Migration.Cli.Commands;
using Xunit;

namespace PPDS.Migration.Cli.Tests.Commands;

public class ImportCommandTests
{
    private readonly Command _command;

    public ImportCommandTests()
    {
        _command = ImportCommand.Create();
    }

    #region Command Structure Tests

    [Fact]
    public void Create_ReturnsCommandWithCorrectName()
    {
        Assert.Equal("import", _command.Name);
    }

    [Fact]
    public void Create_ReturnsCommandWithDescription()
    {
        Assert.Equal("Import data from a ZIP file into Dataverse", _command.Description);
    }

    [Fact]
    public void Create_HasConnectionOption()
    {
        var option = _command.Options.FirstOrDefault(o => o.Name == "connection");
        Assert.NotNull(option);
        // Not required at parse time - can come from environment variable
        Assert.False(option.IsRequired);
        Assert.Contains("-c", option.Aliases);
        Assert.Contains("--connection", option.Aliases);
    }

    [Fact]
    public void Create_HasRequiredDataOption()
    {
        var option = _command.Options.FirstOrDefault(o => o.Name == "data");
        Assert.NotNull(option);
        Assert.True(option.IsRequired);
        Assert.Contains("-d", option.Aliases);
        Assert.Contains("--data", option.Aliases);
    }

    [Fact]
    public void Create_HasOptionalBatchSizeOption()
    {
        var option = _command.Options.FirstOrDefault(o => o.Name == "batch-size");
        Assert.NotNull(option);
        Assert.False(option.IsRequired);
    }

    [Fact]
    public void Create_HasOptionalBypassPluginsOption()
    {
        var option = _command.Options.FirstOrDefault(o => o.Name == "bypass-plugins");
        Assert.NotNull(option);
        Assert.False(option.IsRequired);
    }

    [Fact]
    public void Create_HasOptionalBypassFlowsOption()
    {
        var option = _command.Options.FirstOrDefault(o => o.Name == "bypass-flows");
        Assert.NotNull(option);
        Assert.False(option.IsRequired);
    }

    [Fact]
    public void Create_HasOptionalContinueOnErrorOption()
    {
        var option = _command.Options.FirstOrDefault(o => o.Name == "continue-on-error");
        Assert.NotNull(option);
        Assert.False(option.IsRequired);
    }

    [Fact]
    public void Create_HasOptionalModeOption()
    {
        var option = _command.Options.FirstOrDefault(o => o.Name == "mode");
        Assert.NotNull(option);
        Assert.False(option.IsRequired);
    }

    [Fact]
    public void Create_HasOptionalJsonOption()
    {
        var option = _command.Options.FirstOrDefault(o => o.Name == "json");
        Assert.NotNull(option);
        Assert.False(option.IsRequired);
    }

    [Fact]
    public void Create_HasOptionalVerboseOption()
    {
        var option = _command.Options.FirstOrDefault(o => o.Name == "verbose");
        Assert.NotNull(option);
        Assert.False(option.IsRequired);
        Assert.Contains("-v", option.Aliases);
        Assert.Contains("--verbose", option.Aliases);
    }

    #endregion

    #region Argument Parsing Tests

    [Fact]
    public void Parse_WithAllRequiredOptions_Succeeds()
    {
        var result = _command.Parse("--connection conn --data data.zip");
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Parse_WithShortAliases_Succeeds()
    {
        var result = _command.Parse("-c conn -d data.zip");
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Parse_MissingConnection_NoParseError()
    {
        // Connection can come from environment variable, so no parse error
        var result = _command.Parse("--data data.zip");
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Parse_MissingData_HasError()
    {
        var result = _command.Parse("--connection conn");
        Assert.NotEmpty(result.Errors);
    }

    [Fact]
    public void Parse_WithOptionalBatchSize_Succeeds()
    {
        var result = _command.Parse("-c conn -d data.zip --batch-size 500");
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Parse_WithOptionalBypassPlugins_Succeeds()
    {
        var result = _command.Parse("-c conn -d data.zip --bypass-plugins");
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Parse_WithOptionalBypassFlows_Succeeds()
    {
        var result = _command.Parse("-c conn -d data.zip --bypass-flows");
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Parse_WithOptionalContinueOnError_Succeeds()
    {
        var result = _command.Parse("-c conn -d data.zip --continue-on-error");
        Assert.Empty(result.Errors);
    }

    [Theory]
    [InlineData("Create")]
    [InlineData("Update")]
    [InlineData("Upsert")]
    public void Parse_WithValidMode_Succeeds(string mode)
    {
        var result = _command.Parse($"-c conn -d data.zip --mode {mode}");
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Parse_WithInvalidMode_HasError()
    {
        var result = _command.Parse("-c conn -d data.zip --mode Invalid");
        Assert.NotEmpty(result.Errors);
    }

    [Fact]
    public void Parse_WithAllBypassOptions_Succeeds()
    {
        var result = _command.Parse("-c conn -d data.zip --bypass-plugins --bypass-flows");
        Assert.Empty(result.Errors);
    }

    #endregion
}
