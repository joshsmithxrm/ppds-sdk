using System.CommandLine;

namespace PPDS.Cli.Commands.Data;

/// <summary>
/// Data command group for export, import, copy, and analyze operations.
/// </summary>
public static class DataCommandGroup
{
    /// <summary>
    /// Creates the 'data' command group with all subcommands.
    /// </summary>
    public static Command Create()
    {
        var command = new Command("data", "Data operations: export, import, copy, analyze");

        command.Subcommands.Add(ExportCommand.Create());
        command.Subcommands.Add(ImportCommand.Create());
        command.Subcommands.Add(CopyCommand.Create());
        command.Subcommands.Add(AnalyzeCommand.Create());

        return command;
    }
}
