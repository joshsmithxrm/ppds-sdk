namespace PPDS.Migration.Cli.Commands;

/// <summary>
/// Resolves connection configuration from command-line arguments or environment variables.
/// Uses typed configuration properties instead of connection strings.
/// </summary>
public static class ConnectionResolver
{
    /// <summary>
    /// Environment variable name for the Dataverse URL.
    /// </summary>
    public const string UrlEnvVar = "PPDS_URL";

    /// <summary>
    /// Environment variable name for the client ID.
    /// </summary>
    public const string ClientIdEnvVar = "PPDS_CLIENT_ID";

    /// <summary>
    /// Environment variable name for the client secret.
    /// </summary>
    public const string ClientSecretEnvVar = "PPDS_CLIENT_SECRET";

    /// <summary>
    /// Environment variable name for the tenant ID.
    /// </summary>
    public const string TenantIdEnvVar = "PPDS_TENANT_ID";

    /// <summary>
    /// Environment variable prefix for source environment.
    /// </summary>
    public const string SourcePrefix = "PPDS_SOURCE_";

    /// <summary>
    /// Environment variable prefix for target environment.
    /// </summary>
    public const string TargetPrefix = "PPDS_TARGET_";

    /// <summary>
    /// Connection configuration resolved from environment or arguments.
    /// </summary>
    public record ConnectionConfig(string Url, string ClientId, string ClientSecret, string? TenantId);

    /// <summary>
    /// Resolves connection configuration from environment variables.
    /// </summary>
    /// <param name="prefix">Optional prefix for environment variable names (e.g., "PPDS_SOURCE_").</param>
    /// <param name="connectionName">A friendly name for error messages.</param>
    /// <returns>The resolved connection configuration.</returns>
    public static ConnectionConfig Resolve(string? prefix = null, string connectionName = "connection")
    {
        var urlVar = string.IsNullOrEmpty(prefix) ? UrlEnvVar : $"{prefix}URL";
        var clientIdVar = string.IsNullOrEmpty(prefix) ? ClientIdEnvVar : $"{prefix}CLIENT_ID";
        var secretVar = string.IsNullOrEmpty(prefix) ? ClientSecretEnvVar : $"{prefix}CLIENT_SECRET";
        var tenantVar = string.IsNullOrEmpty(prefix) ? TenantIdEnvVar : $"{prefix}TENANT_ID";

        var url = Environment.GetEnvironmentVariable(urlVar);
        var clientId = Environment.GetEnvironmentVariable(clientIdVar);
        var clientSecret = Environment.GetEnvironmentVariable(secretVar);
        var tenantId = Environment.GetEnvironmentVariable(tenantVar);

        if (string.IsNullOrWhiteSpace(url))
        {
            throw new InvalidOperationException(
                $"No {connectionName} URL provided. Set the {urlVar} environment variable.");
        }

        if (string.IsNullOrWhiteSpace(clientId))
        {
            throw new InvalidOperationException(
                $"No {connectionName} client ID provided. Set the {clientIdVar} environment variable.");
        }

        if (string.IsNullOrWhiteSpace(clientSecret))
        {
            throw new InvalidOperationException(
                $"No {connectionName} client secret provided. Set the {secretVar} environment variable.");
        }

        return new ConnectionConfig(url, clientId, clientSecret, tenantId);
    }

    /// <summary>
    /// Gets a description of required environment variables for help text.
    /// </summary>
    public static string GetHelpDescription()
    {
        return $"Connection configured via environment variables: {UrlEnvVar}, {ClientIdEnvVar}, {ClientSecretEnvVar}, and optionally {TenantIdEnvVar}.";
    }

    /// <summary>
    /// Gets a description of required environment variables for source connection.
    /// </summary>
    public static string GetSourceHelpDescription()
    {
        return $"Source connection configured via: {SourcePrefix}URL, {SourcePrefix}CLIENT_ID, {SourcePrefix}CLIENT_SECRET, {SourcePrefix}TENANT_ID";
    }

    /// <summary>
    /// Gets a description of required environment variables for target connection.
    /// </summary>
    public static string GetTargetHelpDescription()
    {
        return $"Target connection configured via: {TargetPrefix}URL, {TargetPrefix}CLIENT_ID, {TargetPrefix}CLIENT_SECRET, {TargetPrefix}TENANT_ID";
    }
}
