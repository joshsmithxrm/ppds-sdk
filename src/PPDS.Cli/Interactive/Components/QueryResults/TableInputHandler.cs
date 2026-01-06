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

    /// <summary>Move selection left one column (cell navigation).</summary>
    MoveLeft,

    /// <summary>Move selection right one column (cell navigation).</summary>
    MoveRight,

    /// <summary>Scroll viewport left (previous columns) without changing selection.</summary>
    ScrollLeft,

    /// <summary>Scroll viewport right (next columns) without changing selection.</summary>
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

    /// <summary>Copy current cell content to clipboard.</summary>
    CopyCellContent,

    /// <summary>Switch to detailed record view.</summary>
    SwitchToRecordView,

    /// <summary>Start a new query.</summary>
    NewQuery,

    /// <summary>Go back / exit table view.</summary>
    Escape,

    /// <summary>Exit the entire interactive CLI.</summary>
    Exit,

    /// <summary>Show keyboard shortcuts help overlay.</summary>
    ShowHelp
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
        var hasCtrl = (key.Modifiers & ConsoleModifiers.Control) != 0;
        var noModifiers = key.Modifiers == 0;

        return key.Key switch
        {
            // Cell navigation (arrow keys move cell selection)
            ConsoleKey.UpArrow when !hasCtrl => TableInputAction.MoveUp,
            ConsoleKey.DownArrow when !hasCtrl => TableInputAction.MoveDown,
            ConsoleKey.LeftArrow when !hasCtrl => TableInputAction.MoveLeft,
            ConsoleKey.RightArrow when !hasCtrl => TableInputAction.MoveRight,

            // Viewport scroll (Ctrl+Arrow scrolls without moving selection)
            ConsoleKey.LeftArrow when hasCtrl => TableInputAction.ScrollLeft,
            ConsoleKey.RightArrow when hasCtrl => TableInputAction.ScrollRight,

            // Page navigation
            ConsoleKey.PageUp => TableInputAction.PageUp,
            ConsoleKey.PageDown => TableInputAction.PageDown,
            ConsoleKey.Home => TableInputAction.Home,
            ConsoleKey.End => TableInputAction.End,

            // Tab can also scroll columns
            ConsoleKey.Tab when key.Modifiers == ConsoleModifiers.Shift => TableInputAction.ScrollLeft,
            ConsoleKey.Tab when noModifiers => TableInputAction.ScrollRight,

            // Selection
            ConsoleKey.Enter => TableInputAction.SelectRow,

            // Copy actions - Ctrl+C copies cell content, 'C' copies URL
            ConsoleKey.C when hasCtrl => TableInputAction.CopyCellContent,
            ConsoleKey.C when noModifiers => TableInputAction.CopyUrl,

            // Other actions
            ConsoleKey.O when noModifiers => TableInputAction.OpenInBrowser,
            ConsoleKey.R when noModifiers => TableInputAction.SwitchToRecordView,
            ConsoleKey.N when noModifiers => TableInputAction.NewQuery,

            // Exit
            ConsoleKey.Escape => TableInputAction.Escape,
            ConsoleKey.Q when noModifiers => TableInputAction.Exit,
            ConsoleKey.B when noModifiers => TableInputAction.Escape,

            // Help
            ConsoleKey.Oem2 when key.Modifiers == ConsoleModifiers.Shift => TableInputAction.ShowHelp, // ? key (Shift+/)
            ConsoleKey.F1 => TableInputAction.ShowHelp,

            _ => TableInputAction.None
        };
    }
}
