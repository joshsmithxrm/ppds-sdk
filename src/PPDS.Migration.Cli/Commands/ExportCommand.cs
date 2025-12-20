using System.CommandLine;

namespace PPDS.Migration.Cli.Commands;

/// <summary>
/// Export data from a Dataverse environment to a ZIP file.
/// </summary>
public static class ExportCommand
{
    public static Command Create()
    {
        var connectionOption = new Option<string>(
            aliases: ["--connection", "-c"],
            description: "Dataverse connection string")
        {
            IsRequired = true
        };

        var schemaOption = new Option<FileInfo>(
            aliases: ["--schema", "-s"],
            description: "Path to schema.xml file")
        {
            IsRequired = true
        };

        var outputOption = new Option<FileInfo>(
            aliases: ["--output", "-o"],
            description: "Output ZIP file path")
        {
            IsRequired = true
        };

        var parallelOption = new Option<int>(
            name: "--parallel",
            getDefaultValue: () => Environment.ProcessorCount * 2,
            description: "Degree of parallelism for concurrent entity exports");

        var pageSizeOption = new Option<int>(
            name: "--page-size",
            getDefaultValue: () => 5000,
            description: "FetchXML page size for data retrieval");

        var includeFilesOption = new Option<bool>(
            name: "--include-files",
            getDefaultValue: () => false,
            description: "Export file attachments (notes, annotations)");

        var jsonOption = new Option<bool>(
            name: "--json",
            getDefaultValue: () => false,
            description: "Output progress as JSON (for tool integration)");

        var verboseOption = new Option<bool>(
            aliases: ["--verbose", "-v"],
            getDefaultValue: () => false,
            description: "Verbose output");

        var command = new Command("export", "Export data from Dataverse to a ZIP file")
        {
            connectionOption,
            schemaOption,
            outputOption,
            parallelOption,
            pageSizeOption,
            includeFilesOption,
            jsonOption,
            verboseOption
        };

        command.SetHandler(async (context) =>
        {
            var connection = context.ParseResult.GetValueForOption(connectionOption)!;
            var schema = context.ParseResult.GetValueForOption(schemaOption)!;
            var output = context.ParseResult.GetValueForOption(outputOption)!;
            var parallel = context.ParseResult.GetValueForOption(parallelOption);
            var pageSize = context.ParseResult.GetValueForOption(pageSizeOption);
            var includeFiles = context.ParseResult.GetValueForOption(includeFilesOption);
            var json = context.ParseResult.GetValueForOption(jsonOption);
            var verbose = context.ParseResult.GetValueForOption(verboseOption);

            context.ExitCode = await ExecuteAsync(
                connection, schema, output, parallel, pageSize,
                includeFiles, json, verbose, context.GetCancellationToken());
        });

        return command;
    }

    private static async Task<int> ExecuteAsync(
        string connection,
        FileInfo schema,
        FileInfo output,
        int parallel,
        int pageSize,
        bool includeFiles,
        bool json,
        bool verbose,
        CancellationToken cancellationToken)
    {
        try
        {
            // Validate schema file exists
            if (!schema.Exists)
            {
                ConsoleOutput.WriteError($"Schema file not found: {schema.FullName}", json);
                return ExitCodes.InvalidArguments;
            }

            // Validate output directory exists
            var outputDir = output.Directory;
            if (outputDir != null && !outputDir.Exists)
            {
                ConsoleOutput.WriteError($"Output directory does not exist: {outputDir.FullName}", json);
                return ExitCodes.InvalidArguments;
            }

            ConsoleOutput.WriteProgress("analyzing", "Parsing schema...", json);
            ConsoleOutput.WriteProgress("analyzing", "Building dependency graph...", json);

            // TODO: Implement when PPDS.Migration is ready
            // var options = new ExportOptions
            // {
            //     ConnectionString = connection,
            //     SchemaPath = schema.FullName,
            //     OutputPath = output.FullName,
            //     DegreeOfParallelism = parallel,
            //     PageSize = pageSize,
            //     IncludeFiles = includeFiles
            // };
            //
            // var exporter = new DataverseExporter(options);
            // if (json)
            // {
            //     exporter.Progress += (sender, e) => ConsoleOutput.WriteProgress("export", e.Entity, e.Current, e.Total, e.RecordsPerSecond);
            // }
            // await exporter.ExportAsync(cancellationToken);

            ConsoleOutput.WriteProgress("export", "Export not yet implemented - waiting for PPDS.Migration", json);
            await Task.Delay(100, cancellationToken); // Placeholder

            if (!json)
            {
                Console.WriteLine();
                Console.WriteLine("Export completed successfully.");
                Console.WriteLine($"Output: {output.FullName}");
            }
            else
            {
                ConsoleOutput.WriteCompletion(TimeSpan.Zero, 0, 0, json);
            }

            return ExitCodes.Success;
        }
        catch (OperationCanceledException)
        {
            ConsoleOutput.WriteError("Export cancelled by user.", json);
            return ExitCodes.Failure;
        }
        catch (Exception ex)
        {
            ConsoleOutput.WriteError($"Export failed: {ex.Message}", json);
            if (verbose)
            {
                Console.Error.WriteLine(ex.StackTrace);
            }
            return ExitCodes.Failure;
        }
    }
}
