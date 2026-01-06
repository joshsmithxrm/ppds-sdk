using System.CommandLine;
using PPDS.Cli.Commands.DeploymentSettings;
using Xunit;

namespace PPDS.Cli.Tests.Commands.DeploymentSettings;

public class GenerateCommandTests
{
    private readonly Command _command;

    public GenerateCommandTests()
    {
        _command = GenerateCommand.Create();
    }

    [Fact]
    public void Create_ReturnsCommandWithCorrectName()
    {
        Assert.Equal("generate", _command.Name);
    }

    [Fact]
    public void Create_ReturnsCommandWithDescription()
    {
        Assert.Contains("deployment settings", _command.Description, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Create_HasNoArguments()
    {
        Assert.Empty(_command.Arguments);
    }

    [Fact]
    public void Create_HasSolutionOption()
    {
        var option = _command.Options.FirstOrDefault(o => o.Name == "--solution");
        Assert.NotNull(option);
    }

    [Fact]
    public void Create_HasOutputOption()
    {
        var option = _command.Options.FirstOrDefault(o => o.Name == "--output");
        Assert.NotNull(option);
    }

    [Fact]
    public void Create_OutputOptionHasShortAlias()
    {
        var option = _command.Options.First(o => o.Name == "--output");
        Assert.Contains("-o", option.Aliases);
    }

    [Fact]
    public void Create_HasProfileOption()
    {
        var option = _command.Options.FirstOrDefault(o => o.Name == "--profile");
        Assert.NotNull(option);
    }

    [Fact]
    public void Create_HasEnvironmentOption()
    {
        var option = _command.Options.FirstOrDefault(o => o.Name == "--environment");
        Assert.NotNull(option);
    }

    [Fact]
    public void Create_HasGlobalOptions()
    {
        var formatOption = _command.Options.FirstOrDefault(o => o.Name == "--output-format");
        Assert.NotNull(formatOption);
    }
}
