using RadLine;

namespace PPDS.Cli.Interactive;

/// <summary>
/// Extension methods for RadLine LineEditor with PPDS-specific defaults.
/// </summary>
internal static class LineEditorExtensions
{
    /// <summary>
    /// Configures the LineEditor with PPDS default key bindings.
    /// - Escape submits empty input (for "go back" behavior)
    /// </summary>
    /// <param name="editor">The editor to configure.</param>
    /// <param name="onEscape">Optional callback when Escape is pressed.</param>
    /// <returns>The editor for chaining.</returns>
    public static LineEditor WithPpdsDefaults(this LineEditor editor, Action? onEscape = null)
    {
        // Override Escape to submit (go back) instead of clearing line
        editor.KeyBindings.Add(ConsoleKey.Escape, () =>
        {
            onEscape?.Invoke();
            return new SubmitCommand();
        });

        return editor;
    }
}
