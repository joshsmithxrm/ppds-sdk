using PPDS.Auth.Profiles;
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

        var panel = new Panel(content)
        {
            Border = BoxBorder.Rounded,
            BorderStyle = Styles.HeaderBorder,
            Header = new PanelHeader(" PPDS Interactive ", Justify.Center),
            Padding = new Padding(1, 0, 1, 0)
        };

        AnsiConsole.Write(panel);
        AnsiConsole.WriteLine();
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
