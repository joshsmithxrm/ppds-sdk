using PPDS.Auth.Profiles;
using PPDS.Cli.Infrastructure.Errors;
using PPDS.Cli.Interactive.Components;
using PPDS.Cli.Interactive.Selectors;
using PPDS.Cli.Interactive.Wizards;
using Spectre.Console;

namespace PPDS.Cli.Interactive;

/// <summary>
/// Entry point for the interactive TUI mode.
/// </summary>
internal static class InteractiveCli
{
    /// <summary>
    /// Runs the interactive TUI.
    /// </summary>
    /// <returns>Exit code.</returns>
    public static async Task<int> RunAsync()
    {
        // Check if we're in a TTY environment
        if (!AnsiConsole.Profile.Capabilities.Interactive)
        {
            Console.Error.WriteLine("Error: Interactive mode requires a terminal (TTY).");
            Console.Error.WriteLine("This may occur in CI/CD pipelines, redirected input, or non-interactive shells.");
            Console.Error.WriteLine();
            Console.Error.WriteLine("Use standard CLI commands instead:");
            Console.Error.WriteLine("  ppds auth list       - List profiles");
            Console.Error.WriteLine("  ppds auth select     - Select a profile");
            Console.Error.WriteLine("  ppds env list        - List environments");
            Console.Error.WriteLine("  ppds env select      - Select an environment");
            return ExitCodes.Failure;
        }

        using var cts = new CancellationTokenSource();

        // Handle Ctrl+C gracefully
        Console.CancelKeyPress += (_, e) =>
        {
            e.Cancel = true;
            cts.Cancel();
        };

        try
        {
            return await RunMainLoopAsync(cts.Token);
        }
        catch (OperationCanceledException)
        {
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine(Styles.MutedText("Interrupted."));
            return ExitCodes.Success;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine(Styles.ErrorText($"Error: {ex.Message}"));
            return ExitCodes.Failure;
        }
    }

    private static async Task<int> RunMainLoopAsync(CancellationToken cancellationToken)
    {
        using var store = new ProfileStore();

        while (!cancellationToken.IsCancellationRequested)
        {
            // Clear screen for fresh display
            AnsiConsole.Clear();

            // Load profiles
            var collection = await store.LoadAsync(cancellationToken);
            var profile = collection.ActiveProfile;

            // Display context panel
            ContextPanel.Render(profile);

            // Show main menu
            var hasProfile = profile != null;
            var hasEnvironment = profile?.HasEnvironment ?? false;

            var action = MainMenu.Show(hasProfile, hasEnvironment, cancellationToken);

            // Handle action
            var shouldExit = await HandleActionAsync(action, store, collection, cancellationToken);
            if (shouldExit)
            {
                break;
            }
        }

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine(Styles.MutedText("Goodbye!"));
        return ExitCodes.Success;
    }

    private static async Task<bool> HandleActionAsync(
        MainMenu.MenuAction action,
        ProfileStore store,
        ProfileCollection collection,
        CancellationToken cancellationToken)
    {
        switch (action)
        {
            case MainMenu.MenuAction.Exit:
                return true;

            case MainMenu.MenuAction.SwitchProfile:
                await HandleSwitchProfileAsync(store, collection, cancellationToken);
                break;

            case MainMenu.MenuAction.SwitchEnvironment:
                await HandleSwitchEnvironmentAsync(store, collection, cancellationToken);
                break;

            case MainMenu.MenuAction.CreateProfile:
                await HandleCreateProfileAsync(cancellationToken);
                break;

            case MainMenu.MenuAction.SqlQuery:
                if (collection.ActiveProfile != null)
                {
                    await SqlQueryWizard.RunAsync(collection.ActiveProfile, cancellationToken);
                }
                break;

            case MainMenu.MenuAction.DataOperations:
            case MainMenu.MenuAction.PluginManagement:
            case MainMenu.MenuAction.MetadataExplorer:
                ShowComingSoon();
                break;
        }

        return false;
    }

    private static async Task HandleSwitchProfileAsync(
        ProfileStore store,
        ProfileCollection collection,
        CancellationToken cancellationToken)
    {
        AnsiConsole.WriteLine();

        var result = await ProfileSelector.ShowAsync(store, collection, cancellationToken);

        if (result.CreateNew)
        {
            await HandleCreateProfileAsync(cancellationToken);
        }
        else if (result.Changed)
        {
            // Pause briefly to show success message
            await Task.Delay(500, cancellationToken);
        }
    }

    private static async Task HandleSwitchEnvironmentAsync(
        ProfileStore store,
        ProfileCollection collection,
        CancellationToken cancellationToken)
    {
        AnsiConsole.WriteLine();

        var result = await EnvironmentSelector.ShowAsync(store, collection, cancellationToken);

        if (result.ErrorMessage != null)
        {
            AnsiConsole.MarkupLine(Styles.ErrorText(result.ErrorMessage));
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine(Styles.MutedText("Press any key to continue..."));
            Console.ReadKey(true);
        }
        else if (result.Changed)
        {
            // Pause briefly to show success message
            await Task.Delay(500, cancellationToken);
        }
    }

    private static async Task HandleCreateProfileAsync(CancellationToken cancellationToken)
    {
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine(Styles.WarningText("Profile creation requires running the CLI command."));
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("Run one of the following commands:");
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine(Styles.PrimaryText("  ppds auth create"));
        AnsiConsole.MarkupLine(Styles.MutedText("    Interactive browser authentication"));
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine(Styles.PrimaryText("  ppds auth create --deviceCode"));
        AnsiConsole.MarkupLine(Styles.MutedText("    Device code authentication (for SSH/headless)"));
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine(Styles.PrimaryText("  ppds auth create --name <name> --environment <env>"));
        AnsiConsole.MarkupLine(Styles.MutedText("    With profile name and environment"));
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine(Styles.MutedText("Press any key to continue..."));
        Console.ReadKey(true);
        await Task.CompletedTask;
    }

    private static void ShowComingSoon()
    {
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine(Styles.WarningText("This feature is coming soon!"));
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine(Styles.MutedText("Press any key to continue..."));
        Console.ReadKey(true);
    }
}
