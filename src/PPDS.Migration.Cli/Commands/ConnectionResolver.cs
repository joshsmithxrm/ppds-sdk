namespace PPDS.Migration.Cli.Commands;

/// <summary>
/// Resolves connection strings from command-line arguments or environment variables.
/// This helps keep credentials out of command-line arguments where they may be visible
/// in process listings or shell history.
/// </summary>
public static class ConnectionResolver
{
    /// <summary>
    /// Environment variable name for the default connection string.
    /// Used by export and import commands.
    /// </summary>
    public const string ConnectionEnvVar = "PPDS_CONNECTION";

    /// <summary>
    /// Environment variable name for the source connection string.
    /// Used by the migrate command for the source environment.
    /// </summary>
    public const string SourceConnectionEnvVar = "PPDS_SOURCE_CONNECTION";

    /// <summary>
    /// Environment variable name for the target connection string.
    /// Used by the migrate command for the target environment.
    /// </summary>
    public const string TargetConnectionEnvVar = "PPDS_TARGET_CONNECTION";

    /// <summary>
    /// Resolves a connection string from the command-line argument or environment variable.
    /// </summary>
    /// <param name="argumentValue">The value provided via command-line argument (may be null).</param>
    /// <param name="environmentVariable">The environment variable name to check as fallback.</param>
    /// <param name="connectionName">A friendly name for error messages (e.g., "connection", "source", "target").</param>
    /// <returns>The resolved connection string.</returns>
    /// <exception cref="InvalidOperationException">Thrown when no connection string is provided.</exception>
    public static string Resolve(string? argumentValue, string environmentVariable, string connectionName = "connection")
    {
        // Command-line argument takes precedence
        if (!string.IsNullOrWhiteSpace(argumentValue))
        {
            return argumentValue;
        }

        // Fall back to environment variable
        var envValue = Environment.GetEnvironmentVariable(environmentVariable);
        if (!string.IsNullOrWhiteSpace(envValue))
        {
            return envValue;
        }

        throw new InvalidOperationException(
            $"No {connectionName} string provided. " +
            $"Use --{connectionName} argument or set the {environmentVariable} environment variable.");
    }

    /// <summary>
    /// Attempts to resolve a connection string, returning null if not available.
    /// </summary>
    /// <param name="argumentValue">The value provided via command-line argument (may be null).</param>
    /// <param name="environmentVariable">The environment variable name to check as fallback.</param>
    /// <returns>The resolved connection string, or null if not available.</returns>
    public static string? TryResolve(string? argumentValue, string environmentVariable)
    {
        if (!string.IsNullOrWhiteSpace(argumentValue))
        {
            return argumentValue;
        }

        var envValue = Environment.GetEnvironmentVariable(environmentVariable);
        if (!string.IsNullOrWhiteSpace(envValue))
        {
            return envValue;
        }

        return null;
    }

    /// <summary>
    /// Gets a description of where connection strings can be provided for help text.
    /// </summary>
    /// <param name="environmentVariable">The environment variable name.</param>
    /// <returns>A description string for help text.</returns>
    public static string GetHelpDescription(string environmentVariable)
    {
        return $"Dataverse connection string. Can also be set via {environmentVariable} environment variable.";
    }
}
