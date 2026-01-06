using System.CommandLine;

namespace PPDS.Cli.Commands.Users;

/// <summary>
/// Command group for user operations.
/// </summary>
public static class UsersCommandGroup
{
    /// <summary>
    /// Shared profile option for all user commands.
    /// </summary>
    public static readonly Option<string?> ProfileOption = new("--profile", "-p")
    {
        Description = "Authentication profile to use"
    };

    /// <summary>
    /// Shared environment option for all user commands.
    /// </summary>
    public static readonly Option<string?> EnvironmentOption = new("--environment", "-e")
    {
        Description = "Target environment (name, URL, or ID)"
    };

    /// <summary>
    /// Creates the users command group.
    /// </summary>
    public static Command Create()
    {
        var command = new Command("users", "Manage users")
        {
            ListCommand.Create(),
            ShowCommand.Create(),
            RolesCommand.Create()
        };

        return command;
    }
}
