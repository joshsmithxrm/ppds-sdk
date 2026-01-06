using System.CommandLine;
using System.Text.Json.Serialization;
using Microsoft.Extensions.DependencyInjection;
using PPDS.Cli.Infrastructure;
using PPDS.Cli.Infrastructure.Errors;
using PPDS.Cli.Infrastructure.Output;
using PPDS.Dataverse.Services;

namespace PPDS.Cli.Commands.Solutions;

/// <summary>
/// Publish all customizations.
/// </summary>
public static class PublishCommand
{
    public static Command Create()
    {
        var command = new Command("publish", "Publish all customizations")
        {
            SolutionsCommandGroup.ProfileOption,
            SolutionsCommandGroup.EnvironmentOption
        };

        GlobalOptions.AddToCommand(command);

        command.SetAction(async (parseResult, cancellationToken) =>
        {
            var profile = parseResult.GetValue(SolutionsCommandGroup.ProfileOption);
            var environment = parseResult.GetValue(SolutionsCommandGroup.EnvironmentOption);
            var globalOptions = GlobalOptions.GetValues(parseResult);

            return await ExecuteAsync(profile, environment, globalOptions, cancellationToken);
        });

        return command;
    }

    private static async Task<int> ExecuteAsync(
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
                Console.Error.WriteLine("Publishing all customizations...");
            }

            var startTime = DateTime.UtcNow;
            await solutionService.PublishAllAsync(cancellationToken);
            var duration = DateTime.UtcNow - startTime;

            if (globalOptions.IsJsonMode)
            {
                var output = new PublishOutput
                {
                    Success = true,
                    DurationSeconds = duration.TotalSeconds
                };
                writer.WriteSuccess(output);
            }
            else
            {
                Console.Error.WriteLine($"Published successfully in {duration.TotalSeconds:F1} seconds.");
            }

            return ExitCodes.Success;
        }
        catch (Exception ex)
        {
            var error = ExceptionMapper.Map(ex, context: "publishing customizations", debug: globalOptions.Debug);
            writer.WriteError(error);
            return ExceptionMapper.ToExitCode(ex);
        }
    }

    #region Output Models

    private sealed class PublishOutput
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("durationSeconds")]
        public double DurationSeconds { get; set; }
    }

    #endregion
}
