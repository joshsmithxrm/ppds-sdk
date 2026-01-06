using System.CommandLine;
using PPDS.Cli.Commands.ConnectionReferences;
using Xunit;

namespace PPDS.Cli.Tests.Commands.ConnectionReferences;

public class ConnectionsCommandTests
{
    private readonly Command _command;

    public ConnectionsCommandTests()
    {
        _command = ConnectionsCommand.Create();
    }

    [Fact]
    public void Create_ReturnsCommandWithCorrectName()
    {
        Assert.Equal("connections", _command.Name);
    }

    [Fact]
    public void Create_ReturnsCommandWithDescription()
    {
        Assert.Contains("connection", _command.Description, StringComparison.OrdinalIgnoreCase);
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
        Assert.Contains("logical name", arg.Description, StringComparison.OrdinalIgnoreCase);
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
        // Global options include --output-format, --quiet, --verbose, etc.
        var formatOption = _command.Options.FirstOrDefault(o => o.Name == "--output-format");
        Assert.NotNull(formatOption);
    }
}
