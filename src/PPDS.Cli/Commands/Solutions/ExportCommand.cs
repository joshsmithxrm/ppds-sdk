using System.CommandLine;
using System.Text.Json.Serialization;
using Microsoft.Extensions.DependencyInjection;
using PPDS.Cli.Infrastructure;
using PPDS.Cli.Infrastructure.Errors;
using PPDS.Cli.Infrastructure.Output;
using PPDS.Dataverse.Services;

namespace PPDS.Cli.Commands.Solutions;

/// <summary>
/// Export a solution to a ZIP file.
/// </summary>
public static class ExportCommand
{
    public static Command Create()
    {
        var uniqueNameArgument = new Argument<string>("unique-name")
        {
            Description = "The solution unique name"
        };

        var outputOption = new Option<string>("--output", "-o")
        {
            Description = "Output file path (default: <unique-name>.zip or <unique-name>_managed.zip)"
        };

        var managedOption = new Option<bool>("--managed")
        {
            Description = "Export as managed solution"
        };

        var command = new Command("export", "Export a solution to a ZIP file")
        {
            uniqueNameArgument,
            outputOption,
            managedOption,
            SolutionsCommandGroup.ProfileOption,
            SolutionsCommandGroup.EnvironmentOption
        };

        GlobalOptions.AddToCommand(command);

        command.SetAction(async (parseResult, cancellationToken) =>
        {
            var uniqueName = parseResult.GetValue(uniqueNameArgument)!;
            var output = parseResult.GetValue(outputOption);
            var managed = parseResult.GetValue(managedOption);
            var profile = parseResult.GetValue(SolutionsCommandGroup.ProfileOption);
            var environment = parseResult.GetValue(SolutionsCommandGroup.EnvironmentOption);
            var globalOptions = GlobalOptions.GetValues(parseResult);

            return await ExecuteAsync(uniqueName, output, managed, profile, environment, globalOptions, cancellationToken);
        });

        return command;
    }

    private static async Task<int> ExecuteAsync(
        string uniqueName,
        string? outputPath,
        bool managed,
        string? profile,
        string? environment,
        GlobalOptionValues globalOptions,
        CancellationToken cancellationToken)
    {
        var writer = ServiceFactory.CreateOutputWriter(globalOptions);

        try
        {
            await using var serviceProvider = await ProfileServiceFactory.CreateFromProfilesAsync(
                profile,
                environment,
                globalOptions.Verbose,
                globalOptions.Debug,
                ProfileServiceFactory.DefaultDeviceCodeCallback,
                cancellationToken);

            var solutionService = serviceProvider.GetRequiredService<ISolutionService>();

            if (!globalOptions.IsJsonMode)
            {
                var connectionInfo = serviceProvider.GetRequiredService<ResolvedConnectionInfo>();
                ConsoleHeader.WriteConnectedAs(connectionInfo);
                Console.Error.WriteLine();
                Console.Error.WriteLine($"Exporting solution: {uniqueName} ({(managed ? "managed" : "unmanaged")})...");
            }

            // Verify solution exists
            var solution = await solutionService.GetAsync(uniqueName, cancellationToken);
            if (solution == null)
            {
                var error = new StructuredError(
                    ErrorCodes.Operation.NotFound,
                    $"Solution '{uniqueName}' not found.",
                    null,
                    uniqueName);
                writer.WriteError(error);
                return ExitCodes.NotFoundError;
            }

            var solutionZip = await solutionService.ExportAsync(uniqueName, managed, cancellationToken);

            // Determine output path
            var filePath = outputPath ?? $"{uniqueName}{(managed ? "_managed" : "")}.zip";
            var fullPath = Path.GetFullPath(filePath);

            await File.WriteAllBytesAsync(fullPath, solutionZip, cancellationToken);

            if (globalOptions.IsJsonMode)
            {
                var output = new ExportOutput
                {
                    UniqueName = uniqueName,
                    Managed = managed,
                    FilePath = fullPath,
                    FileSizeBytes = solutionZip.Length
                };
                writer.WriteSuccess(output);
            }
            else
            {
                Console.Error.WriteLine($"Exported to: {fullPath}");
                Console.Error.WriteLine($"Size: {FormatBytes(solutionZip.Length)}");
            }

            return ExitCodes.Success;
        }
        catch (Exception ex)
        {
            var error = ExceptionMapper.Map(ex, context: $"exporting solution '{uniqueName}'", debug: globalOptions.Debug);
            writer.WriteError(error);
            return ExceptionMapper.ToExitCode(ex);
        }
    }

    private static string FormatBytes(long bytes)
    {
        string[] sizes = ["B", "KB", "MB", "GB"];
        var order = 0;
        var size = (double)bytes;
        while (size >= 1024 && order < sizes.Length - 1)
        {
            order++;
            size /= 1024;
        }
        return $"{size:0.##} {sizes[order]}";
    }

    #region Output Models

    private sealed class ExportOutput
    {
        [JsonPropertyName("uniqueName")]
        public string UniqueName { get; set; } = string.Empty;

        [JsonPropertyName("managed")]
        public bool Managed { get; set; }

        [JsonPropertyName("filePath")]
        public string FilePath { get; set; } = string.Empty;

        [JsonPropertyName("fileSizeBytes")]
        public long FileSizeBytes { get; set; }
    }

    #endregion
}
