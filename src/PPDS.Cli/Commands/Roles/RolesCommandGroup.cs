using System.CommandLine;

namespace PPDS.Cli.Commands.Roles;

/// <summary>
/// Command group for role operations.
/// </summary>
public static class RolesCommandGroup
{
    /// <summary>
    /// Shared profile option for all role commands.
    /// </summary>
    public static readonly Option<string?> ProfileOption = new("--profile", "-p")
    {
        Description = "Authentication profile to use"
    };

    /// <summary>
    /// Shared environment option for all role commands.
    /// </summary>
    public static readonly Option<string?> EnvironmentOption = new("--environment", "-e")
    {
        Description = "Target environment (name, URL, or ID)"
    };

    /// <summary>
    /// Creates the roles command group.
    /// </summary>
    public static Command Create()
    {
        var command = new Command("roles", "Manage security roles")
        {
            ListCommand.Create(),
            ShowCommand.Create(),
            AssignCommand.Create(),
            RemoveCommand.Create()
        };

        return command;
    }
}
