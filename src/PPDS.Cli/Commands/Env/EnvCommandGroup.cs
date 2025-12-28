using System.CommandLine;
using PPDS.Auth.Cloud;
using PPDS.Auth.Discovery;
using PPDS.Auth.Profiles;
using PPDS.Cli.Commands;

namespace PPDS.Cli.Commands.Env;

/// <summary>
/// Environment management commands.
/// </summary>
public static class EnvCommandGroup
{
    /// <summary>
    /// Creates the 'env' command group with all subcommands.
    /// </summary>
    public static Command Create()
    {
        var command = new Command("env", "Manage environment selection");

        command.Subcommands.Add(CreateListCommand());
        command.Subcommands.Add(CreateSelectCommand());
        command.Subcommands.Add(CreateWhoCommand());

        return command;
    }

    #region List Command

    private static Command CreateListCommand()
    {
        var jsonOption = new Option<bool>("--json")
        {
            Description = "Output as JSON",
            DefaultValueFactory = _ => false
        };

        var command = new Command("list", "List available environments")
        {
            jsonOption
        };

        command.SetAction(async (parseResult, cancellationToken) =>
        {
            var json = parseResult.GetValue(jsonOption);
            return await ExecuteListAsync(json, cancellationToken);
        });

        return command;
    }

    private static async Task<int> ExecuteListAsync(bool json, CancellationToken cancellationToken)
    {
        try
        {
            // Load profiles to get cloud setting
            using var store = new ProfileStore();
            var collection = await store.LoadAsync(cancellationToken);

            var profile = collection.ActiveProfile;
            if (profile == null)
            {
                Console.Error.WriteLine("Error: No active profile. Use 'ppds auth create' first.");
                return ExitCodes.Failure;
            }

            Console.WriteLine("Discovering environments...");
            Console.WriteLine();

            // Use the GlobalDiscoveryService to get environments
            using var gds = GlobalDiscoveryService.FromProfile(profile);
            var environments = await gds.DiscoverEnvironmentsAsync(cancellationToken);

            if (json)
            {
                WriteEnvironmentsAsJson(environments, profile);
            }
            else
            {
                WriteEnvironmentsAsText(environments, profile);
            }

            return ExitCodes.Success;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error: {ex.Message}");
            return ExitCodes.Failure;
        }
    }

    private static void WriteEnvironmentsAsText(
        IReadOnlyList<DiscoveredEnvironment> environments,
        AuthProfile profile)
    {
        if (environments.Count == 0)
        {
            Console.WriteLine("No environments found.");
            Console.WriteLine();
            Console.WriteLine("This may indicate the user has no access to any environments.");
            return;
        }

        Console.WriteLine("Available Environments");
        Console.WriteLine(new string('=', 70));
        Console.WriteLine();

        // Get the currently selected environment URL if any
        var selectedUrl = profile.Environment?.Url?.TrimEnd('/').ToLowerInvariant();

        foreach (var env in environments)
        {
            var isActive = selectedUrl != null &&
                env.ApiUrl.TrimEnd('/').ToLowerInvariant() == selectedUrl;
            var activeMarker = isActive ? " *" : "";

            Console.ForegroundColor = isActive ? ConsoleColor.Green : ConsoleColor.Gray;
            Console.Write($"  {env.FriendlyName}");
            Console.ResetColor();
            Console.WriteLine(activeMarker);

            Console.WriteLine($"      Type: {env.EnvironmentType}");
            Console.WriteLine($"      URL: {env.ApiUrl}");
            Console.WriteLine($"      Unique Name: {env.UniqueName}");
            if (!string.IsNullOrEmpty(env.Region))
            {
                Console.WriteLine($"      Region: {env.Region}");
            }
            Console.WriteLine();
        }

        Console.WriteLine($"Total: {environments.Count} environment(s)");
        if (selectedUrl != null)
        {
            Console.WriteLine("* = active environment");
        }
    }

    private static void WriteEnvironmentsAsJson(
        IReadOnlyList<DiscoveredEnvironment> environments,
        AuthProfile profile)
    {
        var selectedUrl = profile.Environment?.Url?.TrimEnd('/').ToLowerInvariant();

        var output = new
        {
            environments = environments.Select(e => new
            {
                id = e.Id,
                environmentId = e.EnvironmentId,
                friendlyName = e.FriendlyName,
                uniqueName = e.UniqueName,
                apiUrl = e.ApiUrl,
                url = e.Url,
                type = e.EnvironmentType,
                state = e.IsEnabled ? "Enabled" : "Disabled",
                region = e.Region,
                version = e.Version,
                isActive = selectedUrl != null &&
                    e.ApiUrl.TrimEnd('/').ToLowerInvariant() == selectedUrl
            })
        };

        var jsonOutput = System.Text.Json.JsonSerializer.Serialize(output, new System.Text.Json.JsonSerializerOptions
        {
            WriteIndented = true,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });
        Console.WriteLine(jsonOutput);
    }

    #endregion

    #region Select Command

    private static Command CreateSelectCommand()
    {
        var environmentArg = new Argument<string>("environment")
        {
            Description = "Environment name, URL, or unique name"
        };

        var command = new Command("select", "Select the active environment for the current profile")
        {
            environmentArg
        };

        command.SetAction(async (parseResult, cancellationToken) =>
        {
            var environment = parseResult.GetValue(environmentArg)!;
            return await ExecuteSelectAsync(environment, cancellationToken);
        });

        return command;
    }

    private static async Task<int> ExecuteSelectAsync(string environmentIdentifier, CancellationToken cancellationToken)
    {
        try
        {
            using var store = new ProfileStore();
            var collection = await store.LoadAsync(cancellationToken);

            var profile = collection.ActiveProfile;
            if (profile == null)
            {
                Console.Error.WriteLine("Error: No active profile. Use 'ppds auth create' first.");
                return ExitCodes.Failure;
            }

            Console.WriteLine("Discovering environments...");

            // Use the GlobalDiscoveryService to get environments
            using var gds = GlobalDiscoveryService.FromProfile(profile);
            var environments = await gds.DiscoverEnvironmentsAsync(cancellationToken);

            // Resolve the environment
            DiscoveredEnvironment? resolved;
            try
            {
                resolved = EnvironmentResolver.Resolve(environments, environmentIdentifier);
            }
            catch (AmbiguousMatchException ex)
            {
                Console.Error.WriteLine($"Error: {ex.Message}");
                return ExitCodes.Failure;
            }

            if (resolved == null)
            {
                Console.Error.WriteLine($"Error: Environment '{environmentIdentifier}' not found.");
                Console.Error.WriteLine();
                Console.Error.WriteLine("Use 'ppds env list' to see available environments.");
                return ExitCodes.Failure;
            }

            // Update the profile with the selected environment
            profile.Environment = new EnvironmentInfo
            {
                Url = resolved.ApiUrl,
                DisplayName = resolved.FriendlyName,
                UniqueName = resolved.UniqueName,
                EnvironmentId = resolved.EnvironmentId
            };

            await store.SaveAsync(collection, cancellationToken);

            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"Environment selected: {resolved.FriendlyName}");
            Console.ResetColor();
            Console.WriteLine($"  URL: {resolved.ApiUrl}");
            Console.WriteLine($"  Type: {resolved.EnvironmentType}");

            return ExitCodes.Success;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error: {ex.Message}");
            return ExitCodes.Failure;
        }
    }

    #endregion

    #region Who Command

    private static Command CreateWhoCommand()
    {
        var jsonOption = new Option<bool>("--json")
        {
            Description = "Output as JSON",
            DefaultValueFactory = _ => false
        };

        var command = new Command("who", "Show the currently selected environment")
        {
            jsonOption
        };

        command.SetAction(async (parseResult, cancellationToken) =>
        {
            var json = parseResult.GetValue(jsonOption);
            return await ExecuteWhoAsync(json, cancellationToken);
        });

        return command;
    }

    private static async Task<int> ExecuteWhoAsync(bool json, CancellationToken cancellationToken)
    {
        try
        {
            using var store = new ProfileStore();
            var collection = await store.LoadAsync(cancellationToken);

            var profile = collection.ActiveProfile;
            if (profile == null)
            {
                if (json)
                {
                    Console.WriteLine("{\"profile\": null, \"environment\": null}");
                }
                else
                {
                    Console.WriteLine("No active profile.");
                    Console.WriteLine();
                    Console.WriteLine("Use 'ppds auth create' to create a profile.");
                }
                return ExitCodes.Success;
            }

            var env = profile.Environment;
            if (env == null)
            {
                if (json)
                {
                    Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(new
                    {
                        profile = new
                        {
                            index = profile.Index,
                            name = profile.Name,
                            identity = profile.IdentityDisplay
                        },
                        environment = (object?)null
                    }, new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
                }
                else
                {
                    Console.WriteLine($"Profile: {profile.DisplayIdentifier}");
                    Console.WriteLine();
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("No environment selected.");
                    Console.ResetColor();
                    Console.WriteLine();
                    Console.WriteLine("Use 'ppds env select <environment>' to select one.");
                }
                return ExitCodes.Success;
            }

            if (json)
            {
                var output = new
                {
                    profile = new
                    {
                        index = profile.Index,
                        name = profile.Name,
                        identity = profile.IdentityDisplay
                    },
                    environment = new
                    {
                        url = env.Url,
                        displayName = env.DisplayName,
                        uniqueName = env.UniqueName,
                        environmentId = env.EnvironmentId
                    }
                };

                var jsonOutput = System.Text.Json.JsonSerializer.Serialize(output, new System.Text.Json.JsonSerializerOptions
                {
                    WriteIndented = true,
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                });
                Console.WriteLine(jsonOutput);
            }
            else
            {
                Console.WriteLine("Current Environment");
                Console.WriteLine(new string('=', 40));
                Console.WriteLine();
                Console.WriteLine($"  Profile: {profile.DisplayIdentifier}");
                Console.WriteLine($"  Identity: {profile.IdentityDisplay}");
                Console.WriteLine();
                Console.WriteLine($"  Environment: {env.DisplayName}");
                Console.WriteLine($"  URL: {env.Url}");
                if (!string.IsNullOrEmpty(env.UniqueName))
                {
                    Console.WriteLine($"  Unique Name: {env.UniqueName}");
                }
            }

            return ExitCodes.Success;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error: {ex.Message}");
            return ExitCodes.Failure;
        }
    }

    #endregion
}
