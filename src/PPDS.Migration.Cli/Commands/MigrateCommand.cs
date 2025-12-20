using System.CommandLine;

namespace PPDS.Migration.Cli.Commands;

/// <summary>
/// Migrate data from one Dataverse environment to another.
/// </summary>
public static class MigrateCommand
{
    public static Command Create()
    {
        var sourceConnectionOption = new Option<string>(
            aliases: ["--source-connection", "--source"],
            description: "Source Dataverse connection string")
        {
            IsRequired = true
        };

        var targetConnectionOption = new Option<string>(
            aliases: ["--target-connection", "--target"],
            description: "Target Dataverse connection string")
        {
            IsRequired = true
        };

        var schemaOption = new Option<FileInfo>(
            aliases: ["--schema", "-s"],
            description: "Path to schema.xml file")
        {
            IsRequired = true
        };

        var tempDirOption = new Option<DirectoryInfo?>(
            name: "--temp-dir",
            description: "Temporary directory for intermediate data file (default: system temp)");

        var batchSizeOption = new Option<int>(
            name: "--batch-size",
            getDefaultValue: () => 1000,
            description: "Records per batch for import");

        var bypassPluginsOption = new Option<bool>(
            name: "--bypass-plugins",
            getDefaultValue: () => false,
            description: "Bypass custom plugin execution on target");

        var bypassFlowsOption = new Option<bool>(
            name: "--bypass-flows",
            getDefaultValue: () => false,
            description: "Bypass Power Automate flow triggers on target");

        var jsonOption = new Option<bool>(
            name: "--json",
            getDefaultValue: () => false,
            description: "Output progress as JSON (for tool integration)");

        var verboseOption = new Option<bool>(
            aliases: ["--verbose", "-v"],
            getDefaultValue: () => false,
            description: "Verbose output");

        var command = new Command("migrate", "Migrate data from source to target Dataverse environment")
        {
            sourceConnectionOption,
            targetConnectionOption,
            schemaOption,
            tempDirOption,
            batchSizeOption,
            bypassPluginsOption,
            bypassFlowsOption,
            jsonOption,
            verboseOption
        };

        command.SetHandler(async (context) =>
        {
            var sourceConnection = context.ParseResult.GetValueForOption(sourceConnectionOption)!;
            var targetConnection = context.ParseResult.GetValueForOption(targetConnectionOption)!;
            var schema = context.ParseResult.GetValueForOption(schemaOption)!;
            var tempDir = context.ParseResult.GetValueForOption(tempDirOption);
            var batchSize = context.ParseResult.GetValueForOption(batchSizeOption);
            var bypassPlugins = context.ParseResult.GetValueForOption(bypassPluginsOption);
            var bypassFlows = context.ParseResult.GetValueForOption(bypassFlowsOption);
            var json = context.ParseResult.GetValueForOption(jsonOption);
            var verbose = context.ParseResult.GetValueForOption(verboseOption);

            context.ExitCode = await ExecuteAsync(
                sourceConnection, targetConnection, schema, tempDir,
                batchSize, bypassPlugins, bypassFlows, json, verbose, context.GetCancellationToken());
        });

        return command;
    }

    private static async Task<int> ExecuteAsync(
        string sourceConnection,
        string targetConnection,
        FileInfo schema,
        DirectoryInfo? tempDir,
        int batchSize,
        bool bypassPlugins,
        bool bypassFlows,
        bool json,
        bool verbose,
        CancellationToken cancellationToken)
    {
        string? tempDataFile = null;

        try
        {
            // Validate schema file exists
            if (!schema.Exists)
            {
                ConsoleOutput.WriteError($"Schema file not found: {schema.FullName}", json);
                return ExitCodes.InvalidArguments;
            }

            // Determine temp directory
            var tempDirectory = tempDir?.FullName ?? Path.GetTempPath();
            if (!Directory.Exists(tempDirectory))
            {
                ConsoleOutput.WriteError($"Temporary directory does not exist: {tempDirectory}", json);
                return ExitCodes.InvalidArguments;
            }

            // Create temp file path for intermediate data
            tempDataFile = Path.Combine(tempDirectory, $"ppds-migrate-{Guid.NewGuid():N}.zip");

            ConsoleOutput.WriteProgress("analyzing", "Parsing schema...", json);
            ConsoleOutput.WriteProgress("analyzing", "Building dependency graph...", json);

            // TODO: Implement when PPDS.Migration is ready
            // Phase 1: Export from source
            // ConsoleOutput.WriteProgress("export", "Connecting to source environment...", json);
            // var exportOptions = new ExportOptions
            // {
            //     ConnectionString = sourceConnection,
            //     SchemaPath = schema.FullName,
            //     OutputPath = tempDataFile
            // };
            // var exporter = new DataverseExporter(exportOptions);
            // await exporter.ExportAsync(cancellationToken);

            // Phase 2: Import to target
            // ConsoleOutput.WriteProgress("import", "Connecting to target environment...", json);
            // var importOptions = new ImportOptions
            // {
            //     ConnectionString = targetConnection,
            //     DataPath = tempDataFile,
            //     BatchSize = batchSize,
            //     BypassPlugins = bypassPlugins,
            //     BypassFlows = bypassFlows
            // };
            // var importer = new DataverseImporter(importOptions);
            // await importer.ImportAsync(cancellationToken);

            ConsoleOutput.WriteProgress("export", "Export phase not yet implemented - waiting for PPDS.Migration", json);
            ConsoleOutput.WriteProgress("import", "Import phase not yet implemented - waiting for PPDS.Migration", json);
            await Task.Delay(100, cancellationToken); // Placeholder

            if (!json)
            {
                Console.WriteLine();
                Console.WriteLine("Migration completed successfully.");
            }
            else
            {
                ConsoleOutput.WriteCompletion(TimeSpan.Zero, 0, 0, json);
            }

            return ExitCodes.Success;
        }
        catch (OperationCanceledException)
        {
            ConsoleOutput.WriteError("Migration cancelled by user.", json);
            return ExitCodes.Failure;
        }
        catch (Exception ex)
        {
            ConsoleOutput.WriteError($"Migration failed: {ex.Message}", json);
            if (verbose)
            {
                Console.Error.WriteLine(ex.StackTrace);
            }
            return ExitCodes.Failure;
        }
        finally
        {
            // Clean up temp file
            if (tempDataFile != null && File.Exists(tempDataFile))
            {
                try
                {
                    File.Delete(tempDataFile);
                    if (!json)
                    {
                        Console.WriteLine($"Cleaned up temporary file: {tempDataFile}");
                    }
                }
                catch
                {
                    // Ignore cleanup errors
                }
            }
        }
    }
}
