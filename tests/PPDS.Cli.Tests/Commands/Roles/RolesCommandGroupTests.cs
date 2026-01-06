using System.CommandLine;
using PPDS.Cli.Commands.Roles;
using Xunit;

namespace PPDS.Cli.Tests.Commands.Roles;

public class RolesCommandGroupTests
{
    private readonly Command _command;

    public RolesCommandGroupTests()
    {
        _command = RolesCommandGroup.Create();
    }

    #region Command Structure Tests

    [Fact]
    public void Create_ReturnsCommandWithCorrectName()
    {
        Assert.Equal("roles", _command.Name);
    }

    [Fact]
    public void Create_ReturnsCommandWithDescription()
    {
        Assert.Contains("role", _command.Description, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Create_HasListSubcommand()
    {
        var subcommand = _command.Subcommands.FirstOrDefault(c => c.Name == "list");
        Assert.NotNull(subcommand);
    }

    [Fact]
    public void Create_HasShowSubcommand()
    {
        var subcommand = _command.Subcommands.FirstOrDefault(c => c.Name == "show");
        Assert.NotNull(subcommand);
    }

    [Fact]
    public void Create_HasAssignSubcommand()
    {
        var subcommand = _command.Subcommands.FirstOrDefault(c => c.Name == "assign");
        Assert.NotNull(subcommand);
    }

    [Fact]
    public void Create_HasRemoveSubcommand()
    {
        var subcommand = _command.Subcommands.FirstOrDefault(c => c.Name == "remove");
        Assert.NotNull(subcommand);
    }

    [Fact]
    public void Create_HasFourSubcommands()
    {
        Assert.Equal(4, _command.Subcommands.Count);
    }

    #endregion

    #region Shared Options Tests

    [Fact]
    public void ProfileOption_HasCorrectName()
    {
        Assert.Equal("--profile", RolesCommandGroup.ProfileOption.Name);
    }

    [Fact]
    public void ProfileOption_HasShortAlias()
    {
        Assert.Contains("-p", RolesCommandGroup.ProfileOption.Aliases);
    }

    [Fact]
    public void EnvironmentOption_HasCorrectName()
    {
        Assert.Equal("--environment", RolesCommandGroup.EnvironmentOption.Name);
    }

    [Fact]
    public void EnvironmentOption_HasShortAlias()
    {
        Assert.Contains("-e", RolesCommandGroup.EnvironmentOption.Aliases);
    }

    #endregion
}
