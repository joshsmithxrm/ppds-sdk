using System.CommandLine;
using PPDS.Cli.Commands.DeploymentSettings;
using Xunit;

namespace PPDS.Cli.Tests.Commands.DeploymentSettings;

public class ValidateCommandTests
{
    private readonly Command _command;

    public ValidateCommandTests()
    {
        _command = ValidateCommand.Create();
    }

    [Fact]
    public void Create_ReturnsCommandWithCorrectName()
    {
        Assert.Equal("validate", _command.Name);
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
    public void Create_HasFileOption()
    {
        var option = _command.Options.FirstOrDefault(o => o.Name == "--file");
        Assert.NotNull(option);
    }

    [Fact]
    public void Create_FileOptionHasShortAlias()
    {
        var option = _command.Options.First(o => o.Name == "--file");
        Assert.Contains("-f", option.Aliases);
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
