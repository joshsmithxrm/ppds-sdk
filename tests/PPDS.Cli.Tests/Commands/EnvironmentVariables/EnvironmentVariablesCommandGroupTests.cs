using System.CommandLine;
using PPDS.Cli.Commands.EnvironmentVariables;
using Xunit;

namespace PPDS.Cli.Tests.Commands.EnvironmentVariables;

public class EnvironmentVariablesCommandGroupTests
{
    private readonly Command _command;

    public EnvironmentVariablesCommandGroupTests()
    {
        _command = EnvironmentVariablesCommandGroup.Create();
    }

    #region Command Structure Tests

    [Fact]
    public void Create_ReturnsCommandWithCorrectName()
    {
        Assert.Equal("environmentvariables", _command.Name);
    }

    [Fact]
    public void Create_ReturnsCommandWithDescription()
    {
        Assert.Contains("environment variable", _command.Description, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Create_HasListSubcommand()
    {
        var subcommand = _command.Subcommands.FirstOrDefault(c => c.Name == "list");
        Assert.NotNull(subcommand);
    }

    [Fact]
    public void Create_HasGetSubcommand()
    {
        var subcommand = _command.Subcommands.FirstOrDefault(c => c.Name == "get");
        Assert.NotNull(subcommand);
    }

    [Fact]
    public void Create_HasSetSubcommand()
    {
        var subcommand = _command.Subcommands.FirstOrDefault(c => c.Name == "set");
        Assert.NotNull(subcommand);
    }

    [Fact]
    public void Create_HasExportSubcommand()
    {
        var subcommand = _command.Subcommands.FirstOrDefault(c => c.Name == "export");
        Assert.NotNull(subcommand);
    }

    [Fact]
    public void Create_HasUrlSubcommand()
    {
        var subcommand = _command.Subcommands.FirstOrDefault(c => c.Name == "url");
        Assert.NotNull(subcommand);
    }

    [Fact]
    public void Create_HasFiveSubcommands()
    {
        Assert.Equal(5, _command.Subcommands.Count);
    }

    #endregion

    #region Shared Options Tests

    [Fact]
    public void ProfileOption_HasCorrectName()
    {
        Assert.Equal("--profile", EnvironmentVariablesCommandGroup.ProfileOption.Name);
    }

    [Fact]
    public void ProfileOption_HasShortAlias()
    {
        Assert.Contains("-p", EnvironmentVariablesCommandGroup.ProfileOption.Aliases);
    }

    [Fact]
    public void EnvironmentOption_HasCorrectName()
    {
        Assert.Equal("--environment", EnvironmentVariablesCommandGroup.EnvironmentOption.Name);
    }

    [Fact]
    public void EnvironmentOption_HasShortAlias()
    {
        Assert.Contains("-e", EnvironmentVariablesCommandGroup.EnvironmentOption.Aliases);
    }

    #endregion
}
