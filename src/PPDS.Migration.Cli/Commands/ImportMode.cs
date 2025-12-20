namespace PPDS.Migration.Cli.Commands;

/// <summary>
/// Import mode for handling existing records.
/// </summary>
public enum ImportMode
{
    /// <summary>Create new records only. Fails if record exists.</summary>
    Create,

    /// <summary>Update existing records only. Fails if record doesn't exist.</summary>
    Update,

    /// <summary>Create or update records as needed.</summary>
    Upsert
}
