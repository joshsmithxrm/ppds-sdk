using System.CommandLine;
using System.CommandLine.Parsing;
using PPDS.Migration.Cli.Commands;
using Xunit;

namespace PPDS.Migration.Cli.Tests.Commands;

public class AnalyzeCommandTests
{
    private readonly Command _command;

    public AnalyzeCommandTests()
    {
        _command = AnalyzeCommand.Create();
    }

    #region Command Structure Tests

    [Fact]
    public void Create_ReturnsCommandWithCorrectName()
    {
        Assert.Equal("analyze", _command.Name);
    }

    [Fact]
    public void Create_ReturnsCommandWithDescription()
    {
        Assert.Equal("Analyze schema and display dependency graph", _command.Description);
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
    public void Create_HasOptionalOutputFormatOption()
    {
        var option = _command.Options.FirstOrDefault(o => o.Name == "output-format");
        Assert.NotNull(option);
        Assert.False(option.IsRequired);
        Assert.Contains("-f", option.Aliases);
        Assert.Contains("--output-format", option.Aliases);
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
    public void Parse_WithRequiredSchema_Succeeds()
    {
        var result = _command.Parse("--schema schema.xml");
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Parse_WithShortAlias_Succeeds()
    {
        var result = _command.Parse("-s schema.xml");
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Parse_MissingSchema_HasError()
    {
        var result = _command.Parse("");
        Assert.NotEmpty(result.Errors);
    }

    [Theory]
    [InlineData("Text")]
    [InlineData("Json")]
    public void Parse_WithValidOutputFormat_Succeeds(string format)
    {
        var result = _command.Parse($"-s schema.xml --output-format {format}");
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Parse_WithShortOutputFormat_Succeeds()
    {
        var result = _command.Parse("-s schema.xml -f Json");
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Parse_WithInvalidOutputFormat_HasError()
    {
        var result = _command.Parse("-s schema.xml --output-format Invalid");
        Assert.NotEmpty(result.Errors);
    }

    [Fact]
    public void Parse_WithVerbose_Succeeds()
    {
        var result = _command.Parse("-s schema.xml --verbose");
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Parse_WithShortVerbose_Succeeds()
    {
        var result = _command.Parse("-s schema.xml -v");
        Assert.Empty(result.Errors);
    }

    #endregion
}
