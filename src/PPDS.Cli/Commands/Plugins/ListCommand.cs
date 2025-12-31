using System.CommandLine;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.DependencyInjection;
using PPDS.Cli.Infrastructure;
using PPDS.Cli.Plugins.Registration;
using PPDS.Dataverse.Pooling;

namespace PPDS.Cli.Commands.Plugins;

/// <summary>
/// List registered plugins in a Dataverse environment.
/// </summary>
public static class ListCommand
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public static Command Create()
    {
        var assemblyOption = new Option<string?>("--assembly", "-a")
        {
            Description = "Filter by assembly name (classic DLL plugins)"
        };

        var packageOption = new Option<string?>("--package", "-pkg")
        {
            Description = "Filter by package name or unique name (NuGet plugin packages)"
        };

        var command = new Command("list", "List registered plugins in the environment")
        {
            PluginsCommandGroup.ProfileOption,
            PluginsCommandGroup.EnvironmentOption,
            assemblyOption,
            packageOption,
            PluginsCommandGroup.JsonOption
        };

        command.SetAction(async (parseResult, cancellationToken) =>
        {
            var profile = parseResult.GetValue(PluginsCommandGroup.ProfileOption);
            var environment = parseResult.GetValue(PluginsCommandGroup.EnvironmentOption);
            var assembly = parseResult.GetValue(assemblyOption);
            var package = parseResult.GetValue(packageOption);
            var json = parseResult.GetValue(PluginsCommandGroup.JsonOption);

            return await ExecuteAsync(profile, environment, assembly, package, json, cancellationToken);
        });

        return command;
    }

    private static async Task<int> ExecuteAsync(
        string? profile,
        string? environment,
        string? assemblyFilter,
        string? packageFilter,
        bool json,
        CancellationToken cancellationToken)
    {
        try
        {
            await using var serviceProvider = await ProfileServiceFactory.CreateFromProfilesAsync(
                profile,
                environment,
                verbose: false,
                debug: false,
                ProfileServiceFactory.DefaultDeviceCodeCallback,
                cancellationToken);

            var pool = serviceProvider.GetRequiredService<IDataverseConnectionPool>();
            await using var client = await pool.GetClientAsync(cancellationToken: cancellationToken);
            var registrationService = new PluginRegistrationService(client);

            if (!json)
            {
                var connectionInfo = serviceProvider.GetRequiredService<ResolvedConnectionInfo>();
                ConsoleHeader.WriteConnectedAs(connectionInfo);
                Console.WriteLine();
            }

            var output = new ListOutput();

            // Get assemblies (unless package filter is specified, which means we only want packages)
            if (string.IsNullOrEmpty(packageFilter))
            {
                var assemblies = await registrationService.ListAssembliesAsync(assemblyFilter);

                foreach (var assembly in assemblies)
                {
                    var assemblyOutput = new AssemblyOutput
                    {
                        Name = assembly.Name,
                        Version = assembly.Version,
                        PublicKeyToken = assembly.PublicKeyToken,
                        Type = "Assembly",
                        Types = []
                    };

                    var types = await registrationService.ListTypesForAssemblyAsync(assembly.Id);
                    await PopulateTypesAsync(registrationService, types, assemblyOutput.Types);

                    output.Assemblies.Add(assemblyOutput);
                }
            }

            // Get packages (unless assembly filter is specified, which means we only want assemblies)
            if (string.IsNullOrEmpty(assemblyFilter))
            {
                var packages = await registrationService.ListPackagesAsync(packageFilter);

                foreach (var package in packages)
                {
                    var packageOutput = new AssemblyOutput
                    {
                        Name = package.Name,
                        UniqueName = package.UniqueName,
                        Version = package.Version,
                        Type = "Package",
                        Types = []
                    };

                    var types = await registrationService.ListTypesForPackageAsync(package.Id);
                    await PopulateTypesAsync(registrationService, types, packageOutput.Types);

                    output.Packages.Add(packageOutput);
                }
            }

            var totalAssemblies = output.Assemblies.Count;
            var totalPackages = output.Packages.Count;

            if (totalAssemblies == 0 && totalPackages == 0)
            {
                if (json)
                {
                    Console.WriteLine(JsonSerializer.Serialize(output, JsonOptions));
                }
                else
                {
                    Console.WriteLine("No plugin assemblies or packages found.");
                }
                return ExitCodes.Success;
            }

            if (json)
            {
                Console.WriteLine(JsonSerializer.Serialize(output, JsonOptions));
            }
            else
            {
                // Print assemblies
                foreach (var assembly in output.Assemblies)
                {
                    Console.WriteLine($"Assembly: {assembly.Name} (v{assembly.Version})");
                    PrintTypes(assembly.Types);
                    Console.WriteLine();
                }

                // Print packages
                foreach (var package in output.Packages)
                {
                    var uniqueName = package.UniqueName != package.Name ? $" [{package.UniqueName}]" : "";
                    Console.WriteLine($"Package: {package.Name}{uniqueName} (v{package.Version})");
                    PrintTypes(package.Types);
                    Console.WriteLine();
                }

                var totalAssemblySteps = output.Assemblies.Sum(a => a.Types.Sum(t => t.Steps.Count));
                var totalPackageSteps = output.Packages.Sum(p => p.Types.Sum(t => t.Steps.Count));
                Console.WriteLine($"Total: {totalAssemblies} assembly(ies), {totalPackages} package(s), {totalAssemblySteps + totalPackageSteps} step(s)");
            }

            return ExitCodes.Success;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error listing plugins: {ex.Message}");
            return ExitCodes.Failure;
        }
    }

    private static async Task PopulateTypesAsync(
        PluginRegistrationService registrationService,
        List<PluginTypeInfo> types,
        List<TypeOutput> typeOutputs)
    {
        foreach (var type in types)
        {
            var typeOutput = new TypeOutput
            {
                TypeName = type.TypeName,
                Steps = []
            };

            var steps = await registrationService.ListStepsForTypeAsync(type.Id);

            foreach (var step in steps)
            {
                var stepOutput = new StepOutput
                {
                    Name = step.Name,
                    Message = step.Message,
                    Entity = step.PrimaryEntity,
                    Stage = step.Stage,
                    Mode = step.Mode,
                    ExecutionOrder = step.ExecutionOrder,
                    FilteringAttributes = step.FilteringAttributes,
                    IsEnabled = step.IsEnabled,
                    Images = []
                };

                var images = await registrationService.ListImagesForStepAsync(step.Id);

                foreach (var image in images)
                {
                    stepOutput.Images.Add(new ImageOutput
                    {
                        Name = image.Name,
                        ImageType = image.ImageType,
                        Attributes = image.Attributes
                    });
                }

                typeOutput.Steps.Add(stepOutput);
            }

            typeOutputs.Add(typeOutput);
        }
    }

    private static void PrintTypes(List<TypeOutput> types)
    {
        foreach (var type in types)
        {
            Console.WriteLine($"  Type: {type.TypeName}");

            foreach (var step in type.Steps)
            {
                var status = step.IsEnabled ? "" : " [DISABLED]";
                Console.WriteLine($"    Step: {step.Name}{status}");
                Console.WriteLine($"      {step.Message} on {step.Entity} ({step.Stage}, {step.Mode})");

                if (!string.IsNullOrEmpty(step.FilteringAttributes))
                {
                    Console.WriteLine($"      Filtering: {step.FilteringAttributes}");
                }

                foreach (var image in step.Images)
                {
                    var attrs = string.IsNullOrEmpty(image.Attributes) ? "all" : image.Attributes;
                    Console.WriteLine($"      Image: {image.Name} ({image.ImageType}) - {attrs}");
                }
            }
        }
    }

    #region Output Models

    private sealed class ListOutput
    {
        [JsonPropertyName("assemblies")]
        public List<AssemblyOutput> Assemblies { get; set; } = [];

        [JsonPropertyName("packages")]
        public List<AssemblyOutput> Packages { get; set; } = [];
    }

    private sealed class AssemblyOutput
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("uniqueName")]
        public string? UniqueName { get; set; }

        [JsonPropertyName("version")]
        public string? Version { get; set; }

        [JsonPropertyName("publicKeyToken")]
        public string? PublicKeyToken { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; } = "Assembly";

        [JsonPropertyName("types")]
        public List<TypeOutput> Types { get; set; } = [];
    }

    private sealed class TypeOutput
    {
        [JsonPropertyName("typeName")]
        public string TypeName { get; set; } = string.Empty;

        [JsonPropertyName("steps")]
        public List<StepOutput> Steps { get; set; } = [];
    }

    private sealed class StepOutput
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;

        [JsonPropertyName("entity")]
        public string Entity { get; set; } = string.Empty;

        [JsonPropertyName("stage")]
        public string Stage { get; set; } = string.Empty;

        [JsonPropertyName("mode")]
        public string Mode { get; set; } = string.Empty;

        [JsonPropertyName("executionOrder")]
        public int ExecutionOrder { get; set; }

        [JsonPropertyName("filteringAttributes")]
        public string? FilteringAttributes { get; set; }

        [JsonPropertyName("isEnabled")]
        public bool IsEnabled { get; set; }

        [JsonPropertyName("images")]
        public List<ImageOutput> Images { get; set; } = [];
    }

    private sealed class ImageOutput
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("imageType")]
        public string ImageType { get; set; } = string.Empty;

        [JsonPropertyName("attributes")]
        public string? Attributes { get; set; }
    }

    #endregion
}
