using System.CommandLine;
using PPDS.Cli.Commands.Connections;
using Xunit;

namespace PPDS.Cli.Tests.Commands.Connections;

public class GetCommandTests
{
    private readonly Command _command;

    public GetCommandTests()
    {
        _command = GetCommand.Create();
    }

    [Fact]
    public void Create_ReturnsCommandWithCorrectName()
    {
        Assert.Equal("get", _command.Name);
    }

    [Fact]
    public void Create_ReturnsCommandWithDescription()
    {
        Assert.Contains("connection", _command.Description, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Create_HasIdArgument()
    {
        Assert.Single(_command.Arguments);
        Assert.Equal("id", _command.Arguments[0].Name);
    }

    [Fact]
    public void Create_IdArgumentHasDescription()
    {
        var arg = _command.Arguments[0];
        Assert.NotNull(arg.Description);
        Assert.Contains("connection", arg.Description, StringComparison.OrdinalIgnoreCase);
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
