namespace PPDS.Cli.CsvLoader;

/// <summary>
/// Thrown when a mapping file schema version is incompatible with the CLI.
/// </summary>
public sealed class SchemaVersionException : Exception
{
    /// <summary>
    /// The version found in the mapping file.
    /// </summary>
    public string FileVersion { get; }

    /// <summary>
    /// The current CLI version.
    /// </summary>
    public string CliVersion { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="SchemaVersionException"/> class.
    /// </summary>
    public SchemaVersionException(string fileVersion, string cliVersion)
        : base($"Mapping file version {fileVersion} is not compatible with CLI version {cliVersion}. " +
               $"Please upgrade to CLI v{GetMajorVersion(fileVersion)}.x or regenerate the mapping file.")
    {
        FileVersion = fileVersion;
        CliVersion = cliVersion;
    }

    private static string GetMajorVersion(string version)
    {
        var dotIndex = version.IndexOf('.');
        return dotIndex > 0 ? version[..dotIndex] : version;
    }
}
