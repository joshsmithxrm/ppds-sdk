using System.CommandLine;
using System.CommandLine.Parsing;
using PPDS.Migration.Cli.Commands;
using Xunit;

namespace PPDS.Migration.Cli.Tests.Commands;

public class MigrateCommandTests
{
    private readonly Command _command;

    public MigrateCommandTests()
    {
        _command = MigrateCommand.Create();
    }

    #region Command Structure Tests

    [Fact]
    public void Create_ReturnsCommandWithCorrectName()
    {
        Assert.Equal("migrate", _command.Name);
    }

    [Fact]
    public void Create_ReturnsCommandWithDescription()
    {
        Assert.Equal("Migrate data from source to target Dataverse environment", _command.Description);
    }

    [Fact]
    public void Create_HasSourceConnectionOption()
    {
        var option = _command.Options.FirstOrDefault(o => o.Name == "source-connection");
        Assert.NotNull(option);
        // Not required at parse time - can come from environment variable
        Assert.False(option.IsRequired);
        Assert.Contains("--source", option.Aliases);
        Assert.Contains("--source-connection", option.Aliases);
    }

    [Fact]
    public void Create_HasTargetConnectionOption()
    {
        var option = _command.Options.FirstOrDefault(o => o.Name == "target-connection");
        Assert.NotNull(option);
        // Not required at parse time - can come from environment variable
        Assert.False(option.IsRequired);
        Assert.Contains("--target", option.Aliases);
        Assert.Contains("--target-connection", option.Aliases);
    }

    [Fact]
    public void Create_HasRequiredSchemaOption()
    {
        var option = _command.Options.FirstOrDefault(o => o.Name == "schema");
        Assert.NotNull(option);
        Assert.True(option.IsRequired);
        Assert.Contains("-s", option.Aliases);
        Assert.Contains("--schema", option.Aliases);
    }

    [Fact]
    public void Create_HasOptionalTempDirOption()
    {
        var option = _command.Options.FirstOrDefault(o => o.Name == "temp-dir");
        Assert.NotNull(option);
        Assert.False(option.IsRequired);
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
        var result = _command.Parse("--source-connection source --target-connection target --schema schema.xml");
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Parse_WithShortAliases_Succeeds()
    {
        var result = _command.Parse("--source source --target target -s schema.xml");
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Parse_MissingSourceConnection_NoParseError()
    {
        // Connection can come from environment variable, so no parse error
        var result = _command.Parse("--target-connection target --schema schema.xml");
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Parse_MissingTargetConnection_NoParseError()
    {
        // Connection can come from environment variable, so no parse error
        var result = _command.Parse("--source-connection source --schema schema.xml");
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Parse_MissingSchema_HasError()
    {
        var result = _command.Parse("--source-connection source --target-connection target");
        Assert.NotEmpty(result.Errors);
    }

    [Fact]
    public void Parse_WithOptionalTempDir_Succeeds()
    {
        var result = _command.Parse("--source source --target target -s schema.xml --temp-dir /tmp");
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Parse_WithOptionalBatchSize_Succeeds()
    {
        var result = _command.Parse("--source source --target target -s schema.xml --batch-size 500");
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Parse_WithOptionalBypassPlugins_Succeeds()
    {
        var result = _command.Parse("--source source --target target -s schema.xml --bypass-plugins");
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Parse_WithOptionalBypassFlows_Succeeds()
    {
        var result = _command.Parse("--source source --target target -s schema.xml --bypass-flows");
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Parse_WithAllBypassOptions_Succeeds()
    {
        var result = _command.Parse("--source source --target target -s schema.xml --bypass-plugins --bypass-flows");
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Parse_WithOptionalJson_Succeeds()
    {
        var result = _command.Parse("--source source --target target -s schema.xml --json");
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Parse_WithOptionalVerbose_Succeeds()
    {
        var result = _command.Parse("--source source --target target -s schema.xml --verbose");
        Assert.Empty(result.Errors);
    }

    #endregion
}
