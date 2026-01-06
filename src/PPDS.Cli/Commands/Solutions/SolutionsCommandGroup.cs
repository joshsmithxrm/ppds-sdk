using System.CommandLine;

namespace PPDS.Cli.Commands.Solutions;

/// <summary>
/// Solutions command group for managing Dataverse solutions.
/// </summary>
public static class SolutionsCommandGroup
{
    /// <summary>
    /// Profile option for authentication.
    /// </summary>
    public static readonly Option<string?> ProfileOption = new("--profile", "-p")
    {
        Description = "Authentication profile name"
    };

    /// <summary>
    /// Environment option for target environment.
    /// </summary>
    public static readonly Option<string?> EnvironmentOption = new("--environment", "-env")
    {
        Description = "Override the environment URL. Takes precedence over profile's bound environment."
    };

    /// <summary>
    /// Creates the 'solutions' command group with all subcommands.
    /// </summary>
    public static Command Create()
    {
        var command = new Command("solutions", "Manage Dataverse solutions: list, get, export, import, publish");

        command.Subcommands.Add(ListCommand.Create());
        command.Subcommands.Add(GetCommand.Create());
        command.Subcommands.Add(ExportCommand.Create());
        command.Subcommands.Add(ImportCommand.Create());
        command.Subcommands.Add(ComponentsCommand.Create());
        command.Subcommands.Add(PublishCommand.Create());
        command.Subcommands.Add(UrlCommand.Create());

        return command;
    }
}
