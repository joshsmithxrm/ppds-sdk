using System.CommandLine;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace PPDS.Cli.Commands;

/// <summary>
/// Opens CLI documentation in the default browser.
/// </summary>
public static class DocsCommand
{
    /// <summary>
    /// URL to the CLI documentation.
    /// </summary>
    public const string DocsUrl = "https://github.com/joshsmithxrm/ppds-sdk/blob/main/src/PPDS.Cli/README.md";

    /// <summary>
    /// Creates the 'docs' command.
    /// </summary>
    public static Command Create()
    {
        var command = new Command("docs", "Open CLI documentation in browser");

        command.SetAction((parseResult, cancellationToken) =>
        {
            Console.Error.WriteLine($"Opening documentation: {DocsUrl}");
            OpenBrowser(DocsUrl);
            return Task.FromResult(0);
        });

        return command;
    }

    private static void OpenBrowser(string url)
    {
        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                Process.Start("open", url);
            }
            else
            {
                // Linux and other Unix-like systems
                Process.Start("xdg-open", url);
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Could not open browser: {ex.Message}");
            Console.Error.WriteLine($"Please visit: {url}");
        }
    }
}
