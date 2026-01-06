using System.CommandLine;
using PPDS.Cli.Commands.Flows;
using Xunit;

namespace PPDS.Cli.Tests.Commands.Flows;

public class UrlCommandTests
{
    private readonly Command _command;

    public UrlCommandTests()
    {
        _command = UrlCommand.Create();
    }

    [Fact]
    public void Create_ReturnsCommandWithCorrectName()
    {
        Assert.Equal("url", _command.Name);
    }

    [Fact]
    public void Create_ReturnsCommandWithDescription()
    {
        Assert.Contains("url", _command.Description, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Create_HasNameArgument()
    {
        Assert.Single(_command.Arguments);
        Assert.Equal("name", _command.Arguments[0].Name);
    }

    [Fact]
    public void Create_NameArgumentHasDescription()
    {
        var arg = _command.Arguments[0];
        Assert.NotNull(arg.Description);
        Assert.Contains("unique name", arg.Description, StringComparison.OrdinalIgnoreCase);
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
