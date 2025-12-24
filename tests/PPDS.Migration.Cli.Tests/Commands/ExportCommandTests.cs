using System.CommandLine;
using System.CommandLine.Parsing;
using PPDS.Migration.Cli.Commands;
using Xunit;

namespace PPDS.Migration.Cli.Tests.Commands;

public class ExportCommandTests
{
    private readonly Command _command;

    public ExportCommandTests()
    {
        _command = ExportCommand.Create();
    }

    #region Command Structure Tests

    [Fact]
    public void Create_ReturnsCommandWithCorrectName()
    {
        Assert.Equal("export", _command.Name);
    }

    [Fact]
    public void Create_ReturnsCommandWithDescription()
    {
        Assert.StartsWith("Export data from Dataverse to a ZIP file", _command.Description);
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
    public void Create_HasRequiredOutputOption()
    {
        var option = _command.Options.FirstOrDefault(o => o.Name == "output");
        Assert.NotNull(option);
        Assert.True(option.IsRequired);
        Assert.Contains("-o", option.Aliases);
        Assert.Contains("--output", option.Aliases);
    }

    [Fact]
    public void Create_HasOptionalParallelOption()
    {
        var option = _command.Options.FirstOrDefault(o => o.Name == "parallel");
        Assert.NotNull(option);
        Assert.False(option.IsRequired);
    }

    [Fact]
    public void Create_HasOptionalPageSizeOption()
    {
        var option = _command.Options.FirstOrDefault(o => o.Name == "page-size");
        Assert.NotNull(option);
        Assert.False(option.IsRequired);
    }

    [Fact]
    public void Create_HasOptionalIncludeFilesOption()
    {
        var option = _command.Options.FirstOrDefault(o => o.Name == "include-files");
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
        var result = _command.Parse("--schema schema.xml --output data.zip");
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Parse_WithShortAliases_Succeeds()
    {
        var result = _command.Parse("-s schema.xml -o data.zip");
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Parse_MissingSchema_HasError()
    {
        var result = _command.Parse("--output data.zip");
        Assert.NotEmpty(result.Errors);
    }

    [Fact]
    public void Parse_MissingOutput_HasError()
    {
        var result = _command.Parse("--schema schema.xml");
        Assert.NotEmpty(result.Errors);
    }

    [Fact]
    public void Parse_WithOptionalParallel_Succeeds()
    {
        var result = _command.Parse("-s schema.xml -o data.zip --parallel 4");
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Parse_WithOptionalPageSize_Succeeds()
    {
        var result = _command.Parse("-s schema.xml -o data.zip --page-size 1000");
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Parse_WithOptionalIncludeFiles_Succeeds()
    {
        var result = _command.Parse("-s schema.xml -o data.zip --include-files");
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Parse_WithOptionalJson_Succeeds()
    {
        var result = _command.Parse("-s schema.xml -o data.zip --json");
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Parse_WithOptionalVerbose_Succeeds()
    {
        var result = _command.Parse("-s schema.xml -o data.zip --verbose");
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Parse_WithShortVerbose_Succeeds()
    {
        var result = _command.Parse("-s schema.xml -o data.zip -v");
        Assert.Empty(result.Errors);
    }

    #endregion
}
