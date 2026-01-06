using Spectre.Console;

namespace PPDS.Cli.Interactive.Components;

/// <summary>
/// Shared styling constants for the interactive TUI.
/// </summary>
internal static class Styles
{
    /// <summary>
    /// Primary brand color (teal/cyan).
    /// </summary>
    public static readonly Color Primary = new(0, 170, 170);

    /// <summary>
    /// Secondary/accent color (blue).
    /// </summary>
    public static readonly Color Secondary = new(86, 156, 214);

    /// <summary>
    /// Success/active color (green).
    /// </summary>
    public static readonly Color Success = new(78, 201, 176);

    /// <summary>
    /// Warning color (yellow).
    /// </summary>
    public static readonly Color Warning = new(220, 220, 170);

    /// <summary>
    /// Error color (red).
    /// </summary>
    public static readonly Color Error = new(244, 71, 71);

    /// <summary>
    /// Muted/disabled text color (gray).
    /// </summary>
    public static readonly Color Muted = new(128, 128, 128);

    /// <summary>
    /// Style for the header panel border.
    /// </summary>
    public static readonly Style HeaderBorder = new(Primary);

    /// <summary>
    /// Style for highlighted/selected items.
    /// </summary>
    public static readonly Style Highlight = new(Secondary, decoration: Decoration.Bold);

    /// <summary>
    /// Style for active/success items.
    /// </summary>
    public static readonly Style Active = new(Success);

    /// <summary>
    /// Style for disabled/unavailable items.
    /// </summary>
    public static readonly Style Disabled = new(Muted, decoration: Decoration.Dim);

    /// <summary>
    /// Selection prompt highlight style.
    /// </summary>
    public static readonly Style SelectionHighlight = new(Primary, decoration: Decoration.Bold);

    /// <summary>
    /// Creates markup text with the primary color.
    /// </summary>
    public static string PrimaryText(string text) => $"[{Primary.ToMarkup()}]{Markup.Escape(text)}[/]";

    /// <summary>
    /// Creates markup text with the success color.
    /// </summary>
    public static string SuccessText(string text) => $"[{Success.ToMarkup()}]{Markup.Escape(text)}[/]";

    /// <summary>
    /// Creates markup text with the warning color.
    /// </summary>
    public static string WarningText(string text) => $"[{Warning.ToMarkup()}]{Markup.Escape(text)}[/]";

    /// <summary>
    /// Creates markup text with the error color.
    /// </summary>
    public static string ErrorText(string text) => $"[{Error.ToMarkup()}]{Markup.Escape(text)}[/]";

    /// <summary>
    /// Creates markup text with the muted color.
    /// </summary>
    public static string MutedText(string text) => $"[{Muted.ToMarkup()}]{Markup.Escape(text)}[/]";

    /// <summary>
    /// Creates bold markup text.
    /// </summary>
    public static string BoldText(string text) => $"[bold]{Markup.Escape(text)}[/]";
}
