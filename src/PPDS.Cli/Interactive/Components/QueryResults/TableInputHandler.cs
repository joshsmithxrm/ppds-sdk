namespace PPDS.Cli.Interactive.Components.QueryResults;

/// <summary>
/// Actions that can be performed in the interactive table.
/// </summary>
internal enum TableInputAction
{
    /// <summary>No action (unknown key).</summary>
    None,

    /// <summary>Move selection up one row.</summary>
    MoveUp,

    /// <summary>Move selection down one row.</summary>
    MoveDown,

    /// <summary>Scroll viewport left (previous columns).</summary>
    ScrollLeft,

    /// <summary>Scroll viewport right (next columns).</summary>
    ScrollRight,

    /// <summary>Select the current row (open in record view).</summary>
    SelectRow,

    /// <summary>Page up (move up by visible row count).</summary>
    PageUp,

    /// <summary>Page down (move down by visible row count).</summary>
    PageDown,

    /// <summary>Jump to first row.</summary>
    Home,

    /// <summary>Jump to last row.</summary>
    End,

    /// <summary>Open current record in browser.</summary>
    OpenInBrowser,

    /// <summary>Copy record URL to clipboard.</summary>
    CopyUrl,

    /// <summary>Switch to detailed record view.</summary>
    SwitchToRecordView,

    /// <summary>Start a new query.</summary>
    NewQuery,

    /// <summary>Go back / exit table view.</summary>
    Escape,

    /// <summary>Exit the entire interactive CLI.</summary>
    Exit
}

/// <summary>
/// Handles keyboard input for the interactive table.
/// </summary>
internal static class TableInputHandler
{
    /// <summary>
    /// Reads a key press and returns the corresponding action.
    /// </summary>
    public static TableInputAction ReadInput()
    {
        var key = Console.ReadKey(intercept: true);

        return key.Key switch
        {
            // Navigation
            ConsoleKey.UpArrow => TableInputAction.MoveUp,
            ConsoleKey.DownArrow => TableInputAction.MoveDown,
            ConsoleKey.LeftArrow => TableInputAction.ScrollLeft,
            ConsoleKey.RightArrow => TableInputAction.ScrollRight,

            // Page navigation
            ConsoleKey.PageUp => TableInputAction.PageUp,
            ConsoleKey.PageDown => TableInputAction.PageDown,
            ConsoleKey.Home => TableInputAction.Home,
            ConsoleKey.End => TableInputAction.End,

            // Selection
            ConsoleKey.Enter => TableInputAction.SelectRow,

            // Actions
            ConsoleKey.O when key.Modifiers == 0 => TableInputAction.OpenInBrowser,
            ConsoleKey.C when key.Modifiers == 0 => TableInputAction.CopyUrl,
            ConsoleKey.R when key.Modifiers == 0 => TableInputAction.SwitchToRecordView,
            ConsoleKey.N when key.Modifiers == 0 => TableInputAction.NewQuery,

            // Exit
            ConsoleKey.Escape => TableInputAction.Escape,
            ConsoleKey.Q when key.Modifiers == 0 => TableInputAction.Exit,
            ConsoleKey.B when key.Modifiers == 0 => TableInputAction.Escape,

            _ => TableInputAction.None
        };
    }
}
