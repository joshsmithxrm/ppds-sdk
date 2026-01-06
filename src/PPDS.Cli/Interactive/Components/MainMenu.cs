using Spectre.Console;

namespace PPDS.Cli.Interactive.Components;

/// <summary>
/// Main navigation menu for the interactive TUI.
/// </summary>
internal static class MainMenu
{
    /// <summary>
    /// Menu item identifiers.
    /// </summary>
    public enum MenuAction
    {
        SwitchProfile,
        SwitchEnvironment,
        CreateProfile,
        SqlQuery,
        DataOperations,
        PluginManagement,
        MetadataExplorer,
        Exit
    }

    /// <summary>
    /// Represents a menu item with its display text and action.
    /// </summary>
    public sealed class MenuItem
    {
        public required string Label { get; init; }
        public required MenuAction Action { get; init; }
        public bool IsEnabled { get; init; } = true;
        public bool IsCategory { get; init; }

        public override string ToString() => Label;
    }

    /// <summary>
    /// Shows the main menu and returns the selected action.
    /// </summary>
    /// <param name="hasProfile">Whether a profile is configured.</param>
    /// <param name="hasEnvironment">Whether an environment is selected.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The selected menu action.</returns>
    public static MenuAction Show(bool hasProfile, bool hasEnvironment, CancellationToken cancellationToken)
    {
        var items = BuildMenuItems(hasProfile, hasEnvironment);

        var prompt = new SelectionPrompt<MenuItem>()
            .Title("[grey]What would you like to do?[/]")
            .PageSize(10)
            .HighlightStyle(Styles.SelectionHighlight)
            .AddChoices(items)
            .UseConverter(FormatMenuItem);

        try
        {
            var selected = AnsiConsole.Prompt(prompt);
            return selected.Action;
        }
        catch (OperationCanceledException)
        {
            return MenuAction.Exit;
        }
    }

    private static List<MenuItem> BuildMenuItems(bool hasProfile, bool hasEnvironment)
    {
        var items = new List<MenuItem>();

        // Profile/Environment actions
        if (hasProfile)
        {
            items.Add(new MenuItem
            {
                Label = "Switch Profile",
                Action = MenuAction.SwitchProfile
            });

            items.Add(new MenuItem
            {
                Label = "Switch Environment",
                Action = MenuAction.SwitchEnvironment,
                IsEnabled = hasProfile
            });
        }
        else
        {
            items.Add(new MenuItem
            {
                Label = "Create Profile",
                Action = MenuAction.CreateProfile
            });
        }

        // SQL Query - enabled when environment is selected
        items.Add(new MenuItem
        {
            Label = "SQL Query",
            Action = MenuAction.SqlQuery,
            IsEnabled = hasEnvironment
        });

        // Future categories (placeholders)
        items.Add(new MenuItem
        {
            Label = "Data Operations",
            Action = MenuAction.DataOperations,
            IsEnabled = false,
            IsCategory = true
        });

        items.Add(new MenuItem
        {
            Label = "Plugin Management",
            Action = MenuAction.PluginManagement,
            IsEnabled = false,
            IsCategory = true
        });

        items.Add(new MenuItem
        {
            Label = "Metadata Explorer",
            Action = MenuAction.MetadataExplorer,
            IsEnabled = false,
            IsCategory = true
        });

        // Exit
        items.Add(new MenuItem
        {
            Label = "Exit",
            Action = MenuAction.Exit
        });

        return items;
    }

    private static string FormatMenuItem(MenuItem item)
    {
        if (!item.IsEnabled)
        {
            return Styles.MutedText($"{item.Label} (coming soon)");
        }

        if (item.IsCategory)
        {
            return $"{item.Label} [grey]>[/]";
        }

        return item.Label;
    }
}
