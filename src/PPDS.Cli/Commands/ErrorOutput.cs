using System.Reflection;

namespace PPDS.Cli.Commands;

/// <summary>
/// Utility for consistent error output formatting.
/// </summary>
public static class ErrorOutput
{
    /// <summary>
    /// Gets the CLI version from assembly information.
    /// </summary>
    public static string Version
    {
        get
        {
            var assembly = Assembly.GetExecutingAssembly();
            var version = assembly.GetName().Version;
            return version != null ? $"{version.Major}.{version.Minor}.{version.Build}" : "0.0.0";
        }
    }

    /// <summary>
    /// Documentation URL.
    /// </summary>
    public const string DocumentationUrl = "https://github.com/joshsmithxrm/ppds-sdk";

    /// <summary>
    /// Issues URL.
    /// </summary>
    public const string IssuesUrl = "https://github.com/joshsmithxrm/ppds-sdk/issues";

    /// <summary>
    /// Writes a formatted error message with version and documentation info.
    /// </summary>
    /// <param name="message">The error message.</param>
    public static void WriteError(string message)
    {
        Console.Error.WriteLine("PPDS CLI");
        Console.Error.WriteLine($"Version: {Version}");
        Console.Error.WriteLine($"Documentation: {DocumentationUrl}");
        Console.Error.WriteLine($"Issues: {IssuesUrl}");
        Console.Error.WriteLine();
        Console.Error.WriteLine($"Error: {message}");
    }

    /// <summary>
    /// Writes a formatted error message from an exception with version and documentation info.
    /// </summary>
    /// <param name="ex">The exception.</param>
    public static void WriteException(Exception ex)
    {
        WriteError(ex.Message);
    }
}
