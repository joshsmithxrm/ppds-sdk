using System;
using System.Collections.Generic;
using PPDS.Dataverse.Sql.Intellisense;
using Terminal.Gui;

namespace PPDS.Cli.Tui.Views;

/// <summary>
/// A <see cref="TextView"/> subclass that renders per-token syntax highlighting.
/// Language-agnostic: the <see cref="ISourceTokenizer"/> and color map are injected.
/// Overrides <see cref="Redraw"/> to apply token-based coloring while preserving
/// all standard <see cref="TextView"/> behavior (selection, cursor, scrolling, undo).
/// </summary>
internal sealed class SyntaxHighlightedTextView : TextView
{
    private readonly ISourceTokenizer _tokenizer;
    private readonly Dictionary<SourceTokenType, Terminal.Gui.Attribute> _colorMap;
    private readonly Terminal.Gui.Attribute _defaultAttr;

    /// <summary>
    /// Initializes a new syntax-highlighted text view.
    /// </summary>
    /// <param name="tokenizer">Produces tokens for the current text content.</param>
    /// <param name="colorMap">Maps token types to Terminal.Gui color attributes.</param>
    public SyntaxHighlightedTextView(
        ISourceTokenizer tokenizer,
        Dictionary<SourceTokenType, Terminal.Gui.Attribute> colorMap)
    {
        _tokenizer = tokenizer ?? throw new ArgumentNullException(nameof(tokenizer));
        _colorMap = colorMap ?? throw new ArgumentNullException(nameof(colorMap));

        // Default text color (used for gaps between tokens and unknown types)
        _defaultAttr = colorMap.TryGetValue(SourceTokenType.Identifier, out var id)
            ? id
            : new Terminal.Gui.Attribute(Color.White, Color.Black);
    }

    /// <inheritdoc />
    public override void Redraw(Rect bounds)
    {
        if (Driver == null)
        {
            base.Redraw(bounds);
            return;
        }

        var fullText = Text?.ToString() ?? string.Empty;

        // Tokenize once per redraw
        IReadOnlyList<SourceToken> tokens;
        try
        {
            tokens = _tokenizer.Tokenize(fullText);
        }
        catch
        {
            // If tokenizer fails, fall back to base rendering
            base.Redraw(bounds);
            return;
        }

        // Build a flat array mapping each character offset → Attribute
        var charColors = BuildColorMap(fullText, tokens);

        // Render line by line
        var lines = fullText.Split('\n');
        var topRow = TopRow;
        var leftCol = LeftColumn;
        var isSelecting = Selecting;
        var selStartCol = SelectionStartColumn;
        var selStartRow = SelectionStartRow;
        var cursorPos = CursorPosition;

        // Compute selection bounds as flat offsets for easy comparison
        int selStart = -1, selEnd = -1;
        if (isSelecting)
        {
            selStart = FlatOffset(lines, selStartRow, selStartCol);
            selEnd = FlatOffset(lines, cursorPos.Y, cursorPos.X);
            if (selStart > selEnd)
                (selStart, selEnd) = (selEnd, selStart);
        }

        // Selection color: inverted (black on cyan, matching TuiColorPalette.Selected)
        var selectionAttr = Driver.MakeAttribute(Color.Black, Color.Cyan);

        var screenRow = 0;
        var flatPos = 0; // track flat offset into fullText

        // Advance flatPos to account for lines before topRow
        for (int i = 0; i < topRow && i < lines.Length; i++)
        {
            flatPos += lines[i].Length + 1; // +1 for \n
        }

        for (int lineIdx = topRow; lineIdx < lines.Length && screenRow < bounds.Height; lineIdx++)
        {
            var line = lines[lineIdx];
            // Strip trailing \r for accurate rendering (Windows line endings)
            if (line.Length > 0 && line[line.Length - 1] == '\r')
                line = line.Substring(0, line.Length - 1);

            var lineStart = flatPos;

            var screenCol = 0;
            for (int charIdx = leftCol; charIdx < line.Length && screenCol < bounds.Width; charIdx++)
            {
                var ch = line[charIdx];
                var absPos = lineStart + charIdx;

                // Selection always wins over syntax colors
                if (isSelecting && absPos >= selStart && absPos < selEnd)
                {
                    Driver.SetAttribute(selectionAttr);
                }
                else if (absPos < charColors.Length)
                {
                    Driver.SetAttribute(charColors[absPos]);
                }
                else
                {
                    Driver.SetAttribute(_defaultAttr);
                }

                // Handle tabs
                if (ch == '\t')
                {
                    var tabWidth = TabWidth > 0 ? TabWidth : 4;
                    var spaces = tabWidth - (screenCol % tabWidth);
                    for (int t = 0; t < spaces && screenCol < bounds.Width; t++)
                    {
                        AddRune(screenCol, screenRow, ' ');
                        screenCol++;
                    }
                }
                else
                {
                    AddRune(screenCol, screenRow, ch);
                    screenCol += System.Rune.ColumnWidth(ch);
                }
            }

            // Clear rest of line
            if (screenCol < bounds.Width)
            {
                Driver.SetAttribute(_defaultAttr);
                for (int c = screenCol; c < bounds.Width; c++)
                    AddRune(c, screenRow, ' ');
            }

            flatPos += lines[lineIdx].Length + 1; // +1 for \n (use original line length before \r strip)
            screenRow++;
        }

        // Clear remaining rows below text
        if (screenRow < bounds.Height)
        {
            Driver.SetAttribute(_defaultAttr);
            for (int r = screenRow; r < bounds.Height; r++)
            {
                for (int c = 0; c < bounds.Width; c++)
                    AddRune(c, r, ' ');
            }
        }

        PositionCursor();
    }

    /// <summary>
    /// Builds a flat array mapping each character position in the source text
    /// to its syntax color attribute.
    /// </summary>
    private Terminal.Gui.Attribute[] BuildColorMap(string text, IReadOnlyList<SourceToken> tokens)
    {
        var map = new Terminal.Gui.Attribute[text.Length];
        Array.Fill(map, _defaultAttr);

        foreach (var token in tokens)
        {
            if (_colorMap.TryGetValue(token.Type, out var attr))
            {
                var end = Math.Min(token.Start + token.Length, text.Length);
                for (int i = token.Start; i < end; i++)
                    map[i] = attr;
            }
        }

        return map;
    }

    /// <summary>
    /// Computes the flat character offset for a (row, col) position.
    /// </summary>
    private static int FlatOffset(string[] lines, int row, int col)
    {
        var offset = 0;
        for (int i = 0; i < row && i < lines.Length; i++)
            offset += lines[i].Length + 1;
        if (row < lines.Length)
            offset += Math.Min(col, lines[row].Length);
        return offset;
    }
}
