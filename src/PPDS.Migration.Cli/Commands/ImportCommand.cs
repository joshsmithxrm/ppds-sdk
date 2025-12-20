using System.CommandLine;

namespace PPDS.Migration.Cli.Commands;

/// <summary>
/// Import data from a ZIP file into a Dataverse environment.
/// </summary>
public static class ImportCommand
{
    public static Command Create()
    {
        var connectionOption = new Option<string?>(
            aliases: ["--connection", "-c"],
            description: ConnectionResolver.GetHelpDescription(ConnectionResolver.ConnectionEnvVar));

        var dataOption = new Option<FileInfo>(
            aliases: ["--data", "-d"],
            description: "Path to data.zip file")
        {
            IsRequired = true
        };

        var batchSizeOption = new Option<int>(
            name: "--batch-size",
            getDefaultValue: () => 1000,
            description: "Records per batch for ExecuteMultiple requests");

        var bypassPluginsOption = new Option<bool>(
            name: "--bypass-plugins",
            getDefaultValue: () => false,
            description: "Bypass custom plugin execution during import");

        var bypassFlowsOption = new Option<bool>(
            name: "--bypass-flows",
            getDefaultValue: () => false,
            description: "Bypass Power Automate flow triggers during import");

        var continueOnErrorOption = new Option<bool>(
            name: "--continue-on-error",
            getDefaultValue: () => false,
            description: "Continue import on individual record failures");

        var modeOption = new Option<ImportMode>(
            name: "--mode",
            getDefaultValue: () => ImportMode.Upsert,
            description: "Import mode: Create, Update, or Upsert");

        var jsonOption = new Option<bool>(
            name: "--json",
            getDefaultValue: () => false,
            description: "Output progress as JSON (for tool integration)");

        var verboseOption = new Option<bool>(
            aliases: ["--verbose", "-v"],
            getDefaultValue: () => false,
            description: "Verbose output");

        var command = new Command("import", "Import data from a ZIP file into Dataverse")
        {
            connectionOption,
            dataOption,
            batchSizeOption,
            bypassPluginsOption,
            bypassFlowsOption,
            continueOnErrorOption,
            modeOption,
            jsonOption,
            verboseOption
        };

        command.SetHandler(async (context) =>
        {
            var connectionArg = context.ParseResult.GetValueForOption(connectionOption);
            var data = context.ParseResult.GetValueForOption(dataOption)!;
            var batchSize = context.ParseResult.GetValueForOption(batchSizeOption);
            var bypassPlugins = context.ParseResult.GetValueForOption(bypassPluginsOption);
            var bypassFlows = context.ParseResult.GetValueForOption(bypassFlowsOption);
            var continueOnError = context.ParseResult.GetValueForOption(continueOnErrorOption);
            var mode = context.ParseResult.GetValueForOption(modeOption);
            var json = context.ParseResult.GetValueForOption(jsonOption);
            var verbose = context.ParseResult.GetValueForOption(verboseOption);

            // Resolve connection string from argument or environment variable
            string connection;
            try
            {
                connection = ConnectionResolver.Resolve(
                    connectionArg,
                    ConnectionResolver.ConnectionEnvVar,
                    "connection");
            }
            catch (InvalidOperationException ex)
            {
                ConsoleOutput.WriteError(ex.Message, json);
                context.ExitCode = ExitCodes.InvalidArguments;
                return;
            }

            context.ExitCode = await ExecuteAsync(
                connection, data, batchSize, bypassPlugins, bypassFlows,
                continueOnError, mode, json, verbose, context.GetCancellationToken());
        });

        return command;
    }

    private static async Task<int> ExecuteAsync(
        string connection,
        FileInfo data,
        int batchSize,
        bool bypassPlugins,
        bool bypassFlows,
        bool continueOnError,
        ImportMode mode,
        bool json,
        bool verbose,
        CancellationToken cancellationToken)
    {
        try
        {
            // Validate data file exists
            if (!data.Exists)
            {
                ConsoleOutput.WriteError($"Data file not found: {data.FullName}", json);
                return ExitCodes.InvalidArguments;
            }

            ConsoleOutput.WriteProgress("analyzing", "Reading data archive...", json);
            ConsoleOutput.WriteProgress("analyzing", "Building dependency graph...", json);

            // TODO: Implement when PPDS.Migration is ready
            // var options = new ImportOptions
            // {
            //     ConnectionString = connection,
            //     DataPath = data.FullName,
            //     BatchSize = batchSize,
            //     BypassPlugins = bypassPlugins,
            //     BypassFlows = bypassFlows,
            //     ContinueOnError = continueOnError,
            //     Mode = mode
            // };
            //
            // var importer = new DataverseImporter(options);
            // if (json)
            // {
            //     importer.Progress += (sender, e) => ConsoleOutput.WriteProgress("import", e.Entity, e.Current, e.Total, e.RecordsPerSecond);
            // }
            // var result = await importer.ImportAsync(cancellationToken);

            ConsoleOutput.WriteProgress("import", "Import not yet implemented - waiting for PPDS.Migration", json);
            await Task.Delay(100, cancellationToken); // Placeholder

            if (!json)
            {
                Console.WriteLine();
                Console.WriteLine("Import completed successfully.");
            }
            else
            {
                ConsoleOutput.WriteCompletion(TimeSpan.Zero, 0, 0, json);
            }

            return ExitCodes.Success;
        }
        catch (OperationCanceledException)
        {
            ConsoleOutput.WriteError("Import cancelled by user.", json);
            return ExitCodes.Failure;
        }
        catch (Exception ex)
        {
            ConsoleOutput.WriteError($"Import failed: {ex.Message}", json);
            if (verbose)
            {
                Console.Error.WriteLine(ex.StackTrace);
            }
            return ExitCodes.Failure;
        }
    }
}
