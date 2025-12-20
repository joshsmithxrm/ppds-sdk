using System.CommandLine;
using System.Text.Json;

namespace PPDS.Migration.Cli.Commands;

/// <summary>
/// Analyze a schema file and display dependency information.
/// </summary>
public static class AnalyzeCommand
{
    public static Command Create()
    {
        var schemaOption = new Option<FileInfo>(
            aliases: ["--schema", "-s"],
            description: "Path to schema.xml file")
        {
            IsRequired = true
        };

        var outputFormatOption = new Option<OutputFormat>(
            aliases: ["--output-format", "-f"],
            getDefaultValue: () => OutputFormat.Text,
            description: "Output format: text or json");

        var verboseOption = new Option<bool>(
            aliases: ["--verbose", "-v"],
            getDefaultValue: () => false,
            description: "Verbose output");

        var command = new Command("analyze", "Analyze schema and display dependency graph")
        {
            schemaOption,
            outputFormatOption,
            verboseOption
        };

        command.SetHandler(async (context) =>
        {
            var schema = context.ParseResult.GetValueForOption(schemaOption)!;
            var outputFormat = context.ParseResult.GetValueForOption(outputFormatOption);
            var verbose = context.ParseResult.GetValueForOption(verboseOption);

            context.ExitCode = await ExecuteAsync(schema, outputFormat, verbose, context.GetCancellationToken());
        });

        return command;
    }

    private static async Task<int> ExecuteAsync(
        FileInfo schema,
        OutputFormat outputFormat,
        bool verbose,
        CancellationToken cancellationToken)
    {
        try
        {
            // Validate schema file exists
            if (!schema.Exists)
            {
                Console.Error.WriteLine($"Error: Schema file not found: {schema.FullName}");
                return ExitCodes.InvalidArguments;
            }

            // TODO: Implement when PPDS.Migration is ready
            // var analyzer = new SchemaAnalyzer();
            // var analysis = await analyzer.AnalyzeAsync(schema.FullName, cancellationToken);

            // Placeholder analysis result
            var analysis = new SchemaAnalysis
            {
                EntityCount = 0,
                DependencyCount = 0,
                CircularReferenceCount = 0,
                Tiers = [],
                DeferredFields = new Dictionary<string, string[]>(),
                ManyToManyRelationships = []
            };

            await Task.CompletedTask; // Placeholder for async operation

            if (outputFormat == OutputFormat.Json)
            {
                WriteJsonOutput(analysis);
            }
            else
            {
                WriteTextOutput(analysis, schema.FullName);
            }

            return ExitCodes.Success;
        }
        catch (OperationCanceledException)
        {
            Console.Error.WriteLine("Analysis cancelled by user.");
            return ExitCodes.Failure;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Analysis failed: {ex.Message}");
            if (verbose)
            {
                Console.Error.WriteLine(ex.StackTrace);
            }
            return ExitCodes.Failure;
        }
    }

    private static void WriteTextOutput(SchemaAnalysis analysis, string schemaPath)
    {
        Console.WriteLine("Schema Analysis");
        Console.WriteLine("===============");
        Console.WriteLine($"Schema: {schemaPath}");
        Console.WriteLine();

        if (analysis.EntityCount == 0)
        {
            Console.WriteLine("Note: Analysis not yet implemented - waiting for PPDS.Migration");
            Console.WriteLine();
            Console.WriteLine("When implemented, this command will display:");
            Console.WriteLine("  - Entity count and dependency count");
            Console.WriteLine("  - Circular reference detection");
            Console.WriteLine("  - Import tier ordering");
            Console.WriteLine("  - Deferred fields for circular dependencies");
            Console.WriteLine("  - Many-to-many relationship mappings");
            return;
        }

        Console.WriteLine($"Entities: {analysis.EntityCount}");
        Console.WriteLine($"Dependencies: {analysis.DependencyCount}");
        Console.WriteLine($"Circular References: {analysis.CircularReferenceCount}");
        Console.WriteLine();

        Console.WriteLine("Import Tiers:");
        foreach (var tier in analysis.Tiers)
        {
            var entities = string.Join(", ", tier.Entities);
            var suffix = tier.HasCircular ? " (circular)" : "";
            Console.WriteLine($"  Tier {tier.Tier}: {entities}{suffix}");
        }
        Console.WriteLine();

        if (analysis.DeferredFields.Count > 0)
        {
            Console.WriteLine("Deferred Fields:");
            foreach (var (entity, fields) in analysis.DeferredFields)
            {
                foreach (var field in fields)
                {
                    Console.WriteLine($"  {entity}.{field}");
                }
            }
            Console.WriteLine();
        }

        if (analysis.ManyToManyRelationships.Length > 0)
        {
            Console.WriteLine("Many-to-Many Relationships:");
            foreach (var relationship in analysis.ManyToManyRelationships)
            {
                Console.WriteLine($"  {relationship}");
            }
        }
    }

    private static void WriteJsonOutput(SchemaAnalysis analysis)
    {
        var output = new
        {
            entityCount = analysis.EntityCount,
            dependencyCount = analysis.DependencyCount,
            circularReferenceCount = analysis.CircularReferenceCount,
            tiers = analysis.Tiers.Select(t => new
            {
                tier = t.Tier,
                entities = t.Entities,
                hasCircular = t.HasCircular
            }),
            deferredFields = analysis.DeferredFields,
            manyToManyRelationships = analysis.ManyToManyRelationships,
            note = analysis.EntityCount == 0
                ? "Analysis not yet implemented - waiting for PPDS.Migration"
                : null
        };

        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };

        Console.WriteLine(JsonSerializer.Serialize(output, options));
    }
}

/// <summary>
/// Schema analysis results.
/// </summary>
internal class SchemaAnalysis
{
    public int EntityCount { get; set; }
    public int DependencyCount { get; set; }
    public int CircularReferenceCount { get; set; }
    public TierInfo[] Tiers { get; set; } = [];
    public Dictionary<string, string[]> DeferredFields { get; set; } = new();
    public string[] ManyToManyRelationships { get; set; } = [];
}

/// <summary>
/// Import tier information.
/// </summary>
internal class TierInfo
{
    public int Tier { get; set; }
    public string[] Entities { get; set; } = [];
    public bool HasCircular { get; set; }
}
