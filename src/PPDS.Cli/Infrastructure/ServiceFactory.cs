using Microsoft.Extensions.DependencyInjection;
using PPDS.Cli.Commands;
using PPDS.Migration.Analysis;
using PPDS.Migration.Formats;
using PPDS.Migration.Progress;

namespace PPDS.Cli.Infrastructure;

/// <summary>
/// Factory for creating configured service providers for CLI commands.
/// </summary>
public static class ServiceFactory
{
    /// <summary>
    /// Creates a progress reporter based on the output format.
    /// </summary>
    /// <param name="outputFormat">The output format.</param>
    /// <param name="operationName">The operation name for completion messages (e.g., "Export", "Import").</param>
    /// <returns>An appropriate progress reporter.</returns>
    /// <remarks>
    /// Progress is written to stderr to keep stdout clean for command results,
    /// enabling piping (e.g., <c>ppds data export | jq</c>) without interference.
    /// </remarks>
    public static IProgressReporter CreateProgressReporter(OutputFormat outputFormat, string operationName = "Operation")
    {
        // Progress goes to stderr to keep stdout clean for results
        IProgressReporter reporter = outputFormat == OutputFormat.Json
            ? new JsonProgressReporter(Console.Error)
            : new ConsoleProgressReporter();

        reporter.OperationName = operationName;
        return reporter;
    }

    /// <summary>
    /// Creates a service provider for offline analysis (no Dataverse connection needed).
    /// Only registers schema reading and dependency analysis services.
    /// </summary>
    /// <returns>A service provider with analysis services registered.</returns>
    public static ServiceProvider CreateAnalysisProvider()
    {
        var services = new ServiceCollection();

        // Only register services needed for offline analysis - no connection pool required
        services.AddTransient<ICmtSchemaReader, CmtSchemaReader>();
        services.AddTransient<IDependencyGraphBuilder, DependencyGraphBuilder>();

        return services.BuildServiceProvider();
    }
}
