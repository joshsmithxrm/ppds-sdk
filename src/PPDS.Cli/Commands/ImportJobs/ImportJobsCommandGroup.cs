using System.CommandLine;

namespace PPDS.Cli.Commands.ImportJobs;

/// <summary>
/// ImportJobs command group for monitoring solution import jobs.
/// </summary>
public static class ImportJobsCommandGroup
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
    /// Creates the 'importjobs' command group with all subcommands.
    /// </summary>
    public static Command Create()
    {
        var command = new Command("importjobs", "Monitor solution import jobs: list, get, data, wait");

        command.Subcommands.Add(ListCommand.Create());
        command.Subcommands.Add(GetCommand.Create());
        command.Subcommands.Add(DataCommand.Create());
        command.Subcommands.Add(WaitCommand.Create());
        command.Subcommands.Add(UrlCommand.Create());

        return command;
    }
}
