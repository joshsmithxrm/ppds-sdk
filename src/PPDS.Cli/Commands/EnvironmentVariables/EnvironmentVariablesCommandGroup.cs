using System.CommandLine;

namespace PPDS.Cli.Commands.EnvironmentVariables;

/// <summary>
/// Command group for environment variable operations.
/// </summary>
public static class EnvironmentVariablesCommandGroup
{
    /// <summary>
    /// Shared profile option for all environment variable commands.
    /// </summary>
    public static readonly Option<string?> ProfileOption = new("--profile", "-p")
    {
        Description = "Authentication profile to use"
    };

    /// <summary>
    /// Shared environment option for all environment variable commands.
    /// </summary>
    public static readonly Option<string?> EnvironmentOption = new("--environment", "-e")
    {
        Description = "Target environment (name, URL, or ID)"
    };

    /// <summary>
    /// Creates the environmentvariables command group.
    /// </summary>
    public static Command Create()
    {
        var command = new Command("environmentvariables", "Manage environment variables")
        {
            ListCommand.Create(),
            GetCommand.Create(),
            SetCommand.Create(),
            ExportCommand.Create(),
            UrlCommand.Create()
        };

        return command;
    }
}
