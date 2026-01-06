using PPDS.Auth.Profiles;
using PPDS.Cli.Commands;
using Spectre.Console;

namespace PPDS.Cli.Interactive.Components;

/// <summary>
/// Renders the context panel showing current profile and environment.
/// </summary>
internal static class ContextPanel
{
    /// <summary>
    /// Renders the context panel to the console.
    /// </summary>
    /// <param name="profile">The active profile, or null if none.</param>
    public static void Render(AuthProfile? profile)
    {
        var content = BuildContent(profile);

        // Include version in header (truncate commit hash if present)
        var version = TruncateVersion(ErrorOutput.Version);
        var headerText = $" PPDS Interactive v{version} ";

        var panel = new Panel(content)
        {
            Border = BoxBorder.Rounded,
            BorderStyle = Styles.HeaderBorder,
            Header = new PanelHeader(headerText, Justify.Center),
            Padding = new Padding(1, 0, 1, 0)
        };

        AnsiConsole.Write(panel);
        AnsiConsole.WriteLine();
    }

    /// <summary>
    /// Truncates the version string to exclude the commit hash for display.
    /// </summary>
    private static string TruncateVersion(string version)
    {
        // Version might be "1.0.0-beta.9+abc1234" - truncate after + symbol
        var plusIndex = version.IndexOf('+');
        if (plusIndex > 0)
        {
            return version[..plusIndex];
        }
        return version;
    }

    private static string BuildContent(AuthProfile? profile)
    {
        if (profile == null)
        {
            return Styles.WarningText("No profile configured") + "\n" +
                   Styles.MutedText("Use 'Create Profile' to get started");
        }

        var lines = new List<string>();

        // Profile line
        var profileLabel = Styles.MutedText("Profile:");
        var profileValue = Styles.SuccessText(profile.DisplayIdentifier);
        lines.Add($"{profileLabel} {profileValue}");

        // Identity line (only if we have one)
        if (!string.IsNullOrEmpty(profile.Username) || !string.IsNullOrEmpty(profile.ApplicationId))
        {
            var identityLabel = Styles.MutedText("Identity:");
            var identityValue = Markup.Escape(profile.IdentityDisplay);
            lines.Add($"{identityLabel} {identityValue}");
        }

        // Environment line
        var envLabel = Styles.MutedText("Environment:");
        if (profile.HasEnvironment)
        {
            var envValue = Markup.Escape(profile.Environment!.DisplayName);
            var urlValue = Styles.MutedText($"({TruncateUrl(profile.Environment.Url)})");
            lines.Add($"{envLabel} {envValue} {urlValue}");
        }
        else
        {
            var envValue = Styles.WarningText("(not selected)");
            lines.Add($"{envLabel} {envValue}");
        }

        return string.Join("\n", lines);
    }

    /// <summary>
    /// Truncates a URL for display (e.g., "https://org.crm.dynamics.com" -> "org.crm...").
    /// </summary>
    private static string TruncateUrl(string? url)
    {
        if (string.IsNullOrEmpty(url))
            return "";

        // Extract host from URL
        if (Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            var host = uri.Host;
            if (host.Length > 25)
            {
                return host[..22] + "...";
            }
            return host;
        }

        return url.Length > 25 ? url[..22] + "..." : url;
    }
}
