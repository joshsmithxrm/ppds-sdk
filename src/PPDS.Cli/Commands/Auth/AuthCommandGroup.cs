using System.CommandLine;
using PPDS.Auth.Cloud;
using PPDS.Auth.Credentials;
using PPDS.Auth.Profiles;
using PPDS.Cli.Commands;

namespace PPDS.Cli.Commands.Auth;

/// <summary>
/// Authentication profile management commands.
/// </summary>
public static class AuthCommandGroup
{
    /// <summary>
    /// Creates the 'auth' command group with all subcommands.
    /// </summary>
    public static Command Create()
    {
        var command = new Command("auth", "Manage authentication profiles");

        command.Subcommands.Add(CreateCreateCommand());
        command.Subcommands.Add(CreateListCommand());
        command.Subcommands.Add(CreateSelectCommand());
        command.Subcommands.Add(CreateDeleteCommand());
        command.Subcommands.Add(CreateUpdateCommand());
        command.Subcommands.Add(CreateNameCommand());
        command.Subcommands.Add(CreateClearCommand());
        command.Subcommands.Add(CreateWhoCommand());

        return command;
    }

    #region Create Command

    private static Command CreateCreateCommand()
    {
        var nameOption = new Option<string?>("--name", "-n")
        {
            Description = "Profile name (optional, max 30 characters)"
        };
        nameOption.Validators.Add(result =>
        {
            var name = result.GetValue(nameOption);
            if (name?.Length > 30)
                result.AddError("Profile name cannot exceed 30 characters");
        });

        var environmentOption = new Option<string?>("--environment", "-e")
        {
            Description = "Environment URL (optional, can be set later with 'env select')"
        };

        var cloudOption = new Option<CloudEnvironment>("--cloud", "-c")
        {
            Description = "Cloud environment",
            DefaultValueFactory = _ => CloudEnvironment.Public
        };

        var tenantOption = new Option<string?>("--tenant", "-t")
        {
            Description = "Tenant ID (optional)"
        };

        var command = new Command("create", "Create a new authentication profile (interactive login)")
        {
            nameOption,
            environmentOption,
            cloudOption,
            tenantOption
        };

        command.SetAction(async (parseResult, cancellationToken) =>
        {
            var name = parseResult.GetValue(nameOption);
            var environment = parseResult.GetValue(environmentOption);
            var cloud = parseResult.GetValue(cloudOption);
            var tenant = parseResult.GetValue(tenantOption);

            return await ExecuteCreateAsync(name, environment, cloud, tenant, cancellationToken);
        });

        return command;
    }

    private static async Task<int> ExecuteCreateAsync(
        string? name,
        string? environmentUrl,
        CloudEnvironment cloud,
        string? tenantId,
        CancellationToken cancellationToken)
    {
        try
        {
            using var store = new ProfileStore();
            var collection = await store.LoadAsync(cancellationToken);

            // Check for duplicate name
            if (!string.IsNullOrWhiteSpace(name) && collection.IsNameInUse(name))
            {
                Console.Error.WriteLine($"Error: Profile name '{name}' is already in use.");
                return ExitCodes.Failure;
            }

            // Create profile
            var profile = new AuthProfile
            {
                Name = name,
                AuthMethod = AuthMethod.DeviceCode,
                Cloud = cloud,
                TenantId = tenantId
            };

            // Add environment if provided
            if (!string.IsNullOrWhiteSpace(environmentUrl))
            {
                profile.Environment = new EnvironmentInfo
                {
                    Url = environmentUrl.TrimEnd('/'),
                    DisplayName = ExtractEnvironmentName(environmentUrl)
                };
            }

            // Authenticate now to verify credentials and get username
            Console.WriteLine("Authenticating...");
            Console.WriteLine();

            // We need to authenticate to an environment to get the identity
            var targetUrl = environmentUrl ?? "https://globaldisco.crm.dynamics.com";

            var provider = new DeviceCodeCredentialProvider(cloud, tenantId);
            try
            {
                var client = await provider.CreateServiceClientAsync(targetUrl, cancellationToken);
                profile.Username = provider.Identity;
                client.Dispose();
            }
            catch (AuthenticationException ex)
            {
                Console.Error.WriteLine($"Error: Authentication failed: {ex.Message}");
                return ExitCodes.Failure;
            }
            finally
            {
                provider.Dispose();
            }

            // Add to collection (auto-selects if first profile)
            collection.Add(profile);
            await store.SaveAsync(collection, cancellationToken);

            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"Profile created: {profile.DisplayIdentifier}");
            Console.ResetColor();
            Console.WriteLine($"  Identity: {profile.IdentityDisplay}");
            Console.WriteLine($"  Cloud: {profile.Cloud}");
            if (profile.HasEnvironment)
            {
                Console.WriteLine($"  Environment: {profile.Environment!.DisplayName}");
            }
            else
            {
                Console.WriteLine($"  Environment: (none - use 'ppds env select' to set)");
            }

            if (collection.ActiveIndex == profile.Index)
            {
                Console.WriteLine();
                Console.WriteLine("This profile is now active.");
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

    #region List Command

    private static Command CreateListCommand()
    {
        var jsonOption = new Option<bool>("--json")
        {
            Description = "Output as JSON",
            DefaultValueFactory = _ => false
        };

        var command = new Command("list", "List all authentication profiles")
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
            using var store = new ProfileStore();
            var collection = await store.LoadAsync(cancellationToken);

            if (json)
            {
                WriteProfilesAsJson(collection);
            }
            else
            {
                WriteProfilesAsText(collection);
            }

            return ExitCodes.Success;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error: {ex.Message}");
            return ExitCodes.Failure;
        }
    }

    private static void WriteProfilesAsText(ProfileCollection collection)
    {
        if (collection.Count == 0)
        {
            Console.WriteLine("No profiles configured.");
            Console.WriteLine();
            Console.WriteLine("Use 'ppds auth create' to create a profile.");
            return;
        }

        Console.WriteLine("Authentication Profiles");
        Console.WriteLine(new string('=', 60));
        Console.WriteLine();

        foreach (var profile in collection.All)
        {
            var isActive = collection.ActiveIndex == profile.Index;
            var activeMarker = isActive ? " *" : "";

            Console.ForegroundColor = isActive ? ConsoleColor.Green : ConsoleColor.Gray;
            Console.Write($"  [{profile.Index}]");
            Console.ResetColor();

            if (profile.HasName)
            {
                Console.Write($" {profile.Name}");
            }

            Console.WriteLine(activeMarker);

            Console.WriteLine($"      Identity: {profile.IdentityDisplay}");
            Console.WriteLine($"      Method: {profile.AuthMethod}");
            Console.WriteLine($"      Cloud: {profile.Cloud}");

            if (profile.HasEnvironment)
            {
                Console.WriteLine($"      Environment: {profile.Environment!.DisplayName}");
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"      Environment: (none)");
                Console.ResetColor();
            }

            Console.WriteLine();
        }

        Console.WriteLine($"Total: {collection.Count} profile(s)");
        Console.WriteLine("* = active profile");
    }

    private static void WriteProfilesAsJson(ProfileCollection collection)
    {
        var output = new
        {
            activeIndex = collection.ActiveIndex,
            profiles = collection.All.Select(p => new
            {
                index = p.Index,
                name = p.Name,
                identity = p.IdentityDisplay,
                authMethod = p.AuthMethod.ToString(),
                cloud = p.Cloud.ToString(),
                environment = p.Environment != null ? new
                {
                    url = p.Environment.Url,
                    displayName = p.Environment.DisplayName
                } : null,
                isActive = collection.ActiveIndex == p.Index,
                createdAt = p.CreatedAt,
                lastUsedAt = p.LastUsedAt
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
        var profileArg = new Argument<string>("profile")
        {
            Description = "Profile name or index"
        };

        var command = new Command("select", "Select the active profile")
        {
            profileArg
        };

        command.SetAction(async (parseResult, cancellationToken) =>
        {
            var profile = parseResult.GetValue(profileArg)!;
            return await ExecuteSelectAsync(profile, cancellationToken);
        });

        return command;
    }

    private static async Task<int> ExecuteSelectAsync(string profileNameOrIndex, CancellationToken cancellationToken)
    {
        try
        {
            using var store = new ProfileStore();
            var collection = await store.LoadAsync(cancellationToken);

            var profile = collection.GetByNameOrIndex(profileNameOrIndex);
            if (profile == null)
            {
                Console.Error.WriteLine($"Error: Profile '{profileNameOrIndex}' not found.");
                Console.Error.WriteLine();
                Console.Error.WriteLine("Use 'ppds auth list' to see available profiles.");
                return ExitCodes.Failure;
            }

            collection.SetActiveByIndex(profile.Index);
            await store.SaveAsync(collection, cancellationToken);

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"Active profile: {profile.DisplayIdentifier}");
            Console.ResetColor();
            Console.WriteLine($"  Identity: {profile.IdentityDisplay}");
            if (profile.HasEnvironment)
            {
                Console.WriteLine($"  Environment: {profile.Environment!.DisplayName}");
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

    #region Delete Command

    private static Command CreateDeleteCommand()
    {
        var profileArg = new Argument<string>("profile")
        {
            Description = "Profile name or index"
        };

        var forceOption = new Option<bool>("--force", "-f")
        {
            Description = "Skip confirmation prompt",
            DefaultValueFactory = _ => false
        };

        var command = new Command("delete", "Delete an authentication profile")
        {
            profileArg,
            forceOption
        };

        command.SetAction(async (parseResult, cancellationToken) =>
        {
            var profile = parseResult.GetValue(profileArg)!;
            var force = parseResult.GetValue(forceOption);
            return await ExecuteDeleteAsync(profile, force, cancellationToken);
        });

        return command;
    }

    private static async Task<int> ExecuteDeleteAsync(string profileNameOrIndex, bool force, CancellationToken cancellationToken)
    {
        try
        {
            using var store = new ProfileStore();
            var collection = await store.LoadAsync(cancellationToken);

            var profile = collection.GetByNameOrIndex(profileNameOrIndex);
            if (profile == null)
            {
                Console.Error.WriteLine($"Error: Profile '{profileNameOrIndex}' not found.");
                return ExitCodes.Failure;
            }

            if (!force)
            {
                Console.WriteLine($"Delete profile '{profile.DisplayIdentifier}'?");
                Console.WriteLine($"  Identity: {profile.IdentityDisplay}");
                Console.Write("Type 'yes' to confirm: ");
                var confirmation = Console.ReadLine();
                if (!string.Equals(confirmation, "yes", StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine("Cancelled.");
                    return ExitCodes.Success;
                }
            }

            collection.RemoveByIndex(profile.Index);
            await store.SaveAsync(collection, cancellationToken);

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"Profile deleted: {profile.DisplayIdentifier}");
            Console.ResetColor();

            if (collection.ActiveProfile != null)
            {
                Console.WriteLine($"Active profile is now: {collection.ActiveProfile.DisplayIdentifier}");
            }
            else if (collection.Count > 0)
            {
                Console.WriteLine("No active profile. Use 'ppds auth select' to set one.");
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

    #region Update Command

    private static Command CreateUpdateCommand()
    {
        var profileArg = new Argument<string?>("profile")
        {
            Description = "Profile name or index (default: active profile)",
            DefaultValueFactory = _ => null,
            Arity = ArgumentArity.ZeroOrOne
        };

        var command = new Command("update", "Re-authenticate an existing profile")
        {
            profileArg
        };

        command.SetAction(async (parseResult, cancellationToken) =>
        {
            var profile = parseResult.GetValue(profileArg);
            return await ExecuteUpdateAsync(profile, cancellationToken);
        });

        return command;
    }

    private static async Task<int> ExecuteUpdateAsync(string? profileNameOrIndex, CancellationToken cancellationToken)
    {
        try
        {
            using var store = new ProfileStore();
            var collection = await store.LoadAsync(cancellationToken);

            AuthProfile profile;
            if (string.IsNullOrWhiteSpace(profileNameOrIndex))
            {
                profile = collection.ActiveProfile
                    ?? throw new InvalidOperationException("No active profile. Specify a profile or use 'ppds auth select' first.");
            }
            else
            {
                profile = collection.GetByNameOrIndex(profileNameOrIndex)
                    ?? throw new InvalidOperationException($"Profile '{profileNameOrIndex}' not found.");
            }

            Console.WriteLine($"Re-authenticating profile: {profile.DisplayIdentifier}");
            Console.WriteLine();

            // Only device code profiles can be updated this way
            if (profile.AuthMethod != AuthMethod.DeviceCode)
            {
                Console.Error.WriteLine($"Error: Cannot update {profile.AuthMethod} profiles interactively.");
                Console.Error.WriteLine("For service principal profiles, delete and recreate with new credentials.");
                return ExitCodes.Failure;
            }

            var targetUrl = profile.Environment?.Url ?? "https://globaldisco.crm.dynamics.com";

            var provider = new DeviceCodeCredentialProvider(profile.Cloud, profile.TenantId);
            try
            {
                var client = await provider.CreateServiceClientAsync(targetUrl, cancellationToken);
                profile.Username = provider.Identity;
                profile.LastUsedAt = DateTimeOffset.UtcNow;
                client.Dispose();
            }
            catch (AuthenticationException ex)
            {
                Console.Error.WriteLine($"Error: Authentication failed: {ex.Message}");
                return ExitCodes.Failure;
            }
            finally
            {
                provider.Dispose();
            }

            await store.SaveAsync(collection, cancellationToken);

            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"Profile updated: {profile.DisplayIdentifier}");
            Console.ResetColor();
            Console.WriteLine($"  Identity: {profile.IdentityDisplay}");

            return ExitCodes.Success;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error: {ex.Message}");
            return ExitCodes.Failure;
        }
    }

    #endregion

    #region Name Command

    private static Command CreateNameCommand()
    {
        var profileArg = new Argument<string>("profile")
        {
            Description = "Profile name or index to rename"
        };

        var newNameArg = new Argument<string>("new-name")
        {
            Description = "New name for the profile (max 30 characters)"
        };

        var command = new Command("name", "Rename a profile")
        {
            profileArg,
            newNameArg
        };

        command.SetAction(async (parseResult, cancellationToken) =>
        {
            var profile = parseResult.GetValue(profileArg)!;
            var newName = parseResult.GetValue(newNameArg)!;
            return await ExecuteNameAsync(profile, newName, cancellationToken);
        });

        return command;
    }

    private static async Task<int> ExecuteNameAsync(string profileNameOrIndex, string newName, CancellationToken cancellationToken)
    {
        try
        {
            if (newName.Length > 30)
            {
                Console.Error.WriteLine("Error: Profile name cannot exceed 30 characters.");
                return ExitCodes.Failure;
            }

            using var store = new ProfileStore();
            var collection = await store.LoadAsync(cancellationToken);

            var profile = collection.GetByNameOrIndex(profileNameOrIndex);
            if (profile == null)
            {
                Console.Error.WriteLine($"Error: Profile '{profileNameOrIndex}' not found.");
                return ExitCodes.Failure;
            }

            if (collection.IsNameInUse(newName, profile.Index))
            {
                Console.Error.WriteLine($"Error: Profile name '{newName}' is already in use.");
                return ExitCodes.Failure;
            }

            var oldName = profile.DisplayIdentifier;
            profile.Name = newName;
            await store.SaveAsync(collection, cancellationToken);

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"Profile renamed: {oldName} -> {profile.DisplayIdentifier}");
            Console.ResetColor();

            return ExitCodes.Success;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error: {ex.Message}");
            return ExitCodes.Failure;
        }
    }

    #endregion

    #region Clear Command

    private static Command CreateClearCommand()
    {
        var forceOption = new Option<bool>("--force", "-f")
        {
            Description = "Skip confirmation prompt",
            DefaultValueFactory = _ => false
        };

        var command = new Command("clear", "Delete all profiles and cached credentials")
        {
            forceOption
        };

        command.SetAction(async (parseResult, cancellationToken) =>
        {
            var force = parseResult.GetValue(forceOption);
            return await ExecuteClearAsync(force, cancellationToken);
        });

        return command;
    }

    private static async Task<int> ExecuteClearAsync(bool force, CancellationToken cancellationToken)
    {
        try
        {
            using var store = new ProfileStore();
            var collection = await store.LoadAsync(cancellationToken);

            if (collection.Count == 0)
            {
                Console.WriteLine("No profiles to clear.");
                return ExitCodes.Success;
            }

            if (!force)
            {
                Console.WriteLine($"This will delete {collection.Count} profile(s) and all cached credentials.");
                Console.Write("Type 'yes' to confirm: ");
                var confirmation = Console.ReadLine();
                if (!string.Equals(confirmation, "yes", StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine("Cancelled.");
                    return ExitCodes.Success;
                }
            }

            store.Delete();

            // Also clear the token cache
            var tokenCachePath = ProfilePaths.TokenCacheFile;
            if (File.Exists(tokenCachePath))
            {
                File.Delete(tokenCachePath);
            }

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("All profiles and cached credentials deleted.");
            Console.ResetColor();

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

        var command = new Command("who", "Show the current active profile")
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
                    Console.WriteLine("{\"active\": null}");
                }
                else
                {
                    Console.WriteLine("No active profile.");
                    Console.WriteLine();
                    Console.WriteLine("Use 'ppds auth create' to create a profile.");
                }
                return ExitCodes.Success;
            }

            if (json)
            {
                var output = new
                {
                    active = new
                    {
                        index = profile.Index,
                        name = profile.Name,
                        identity = profile.IdentityDisplay,
                        authMethod = profile.AuthMethod.ToString(),
                        cloud = profile.Cloud.ToString(),
                        environment = profile.Environment != null ? new
                        {
                            url = profile.Environment.Url,
                            displayName = profile.Environment.DisplayName
                        } : null,
                        createdAt = profile.CreatedAt,
                        lastUsedAt = profile.LastUsedAt
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
                Console.WriteLine("Active Profile");
                Console.WriteLine(new string('=', 40));
                Console.WriteLine();
                Console.WriteLine($"  Profile: {profile.DisplayIdentifier}");
                Console.WriteLine($"  Identity: {profile.IdentityDisplay}");
                Console.WriteLine($"  Method: {profile.AuthMethod}");
                Console.WriteLine($"  Cloud: {profile.Cloud}");

                if (profile.HasEnvironment)
                {
                    Console.WriteLine($"  Environment: {profile.Environment!.DisplayName}");
                    Console.WriteLine($"  URL: {profile.Environment.Url}");
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"  Environment: (none)");
                    Console.ResetColor();
                    Console.WriteLine();
                    Console.WriteLine("Tip: Use 'ppds env select' to set an environment.");
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

    #region Helpers

    private static string ExtractEnvironmentName(string url)
    {
        try
        {
            var uri = new Uri(url);
            var host = uri.Host;

            // Extract org name from host (e.g., "myorg" from "myorg.crm.dynamics.com")
            var parts = host.Split('.');
            if (parts.Length > 0)
            {
                return parts[0];
            }
        }
        catch
        {
            // Ignore parse errors
        }

        return url;
    }

    #endregion
}
