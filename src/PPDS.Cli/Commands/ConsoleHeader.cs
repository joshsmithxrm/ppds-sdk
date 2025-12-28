using PPDS.Auth.Profiles;

namespace PPDS.Cli.Commands;

/// <summary>
/// Utility for writing consistent header messages to the console.
/// </summary>
public static class ConsoleHeader
{
    /// <summary>
    /// Writes the "Connected as" header for API commands.
    /// </summary>
    /// <param name="profile">The active auth profile.</param>
    /// <param name="environmentName">Optional environment name to show on a separate "Connected to..." line.</param>
    public static void WriteConnectedAs(AuthProfile profile, string? environmentName = null)
    {
        var identity = GetIdentityDisplay(profile);
        Console.WriteLine($"Connected as {identity}");

        if (!string.IsNullOrEmpty(environmentName))
        {
            Console.WriteLine($"Connected to... {environmentName}");
        }
    }

    /// <summary>
    /// Gets the display identity for a profile.
    /// </summary>
    /// <param name="profile">The profile.</param>
    /// <returns>The identity display string.</returns>
    public static string GetIdentityDisplay(AuthProfile profile)
    {
        if (!string.IsNullOrEmpty(profile.Username))
            return profile.Username;

        if (!string.IsNullOrEmpty(profile.ApplicationId))
            return $"app:{profile.ApplicationId[..Math.Min(8, profile.ApplicationId.Length)]}...";

        return "(unknown)";
    }
}
