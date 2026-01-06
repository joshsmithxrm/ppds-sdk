using PPDS.Cli.Interactive.Components;
using Xunit;

namespace PPDS.Cli.Tests.Interactive;

/// <summary>
/// Tests for the Styles utility class.
/// </summary>
public class StylesTests
{
    [Fact]
    public void PrimaryText_EscapesMarkup()
    {
        var text = "[test] with <brackets>";
        var result = Styles.PrimaryText(text);

        // Should not contain unescaped brackets
        Assert.Contains("[[test]]", result);
    }

    [Fact]
    public void SuccessText_EscapesMarkup()
    {
        var text = "[test]";
        var result = Styles.SuccessText(text);

        Assert.Contains("[[test]]", result);
    }

    [Fact]
    public void WarningText_EscapesMarkup()
    {
        var text = "[warning]";
        var result = Styles.WarningText(text);

        Assert.Contains("[[warning]]", result);
    }

    [Fact]
    public void ErrorText_EscapesMarkup()
    {
        var text = "[error]";
        var result = Styles.ErrorText(text);

        Assert.Contains("[[error]]", result);
    }

    [Fact]
    public void MutedText_EscapesMarkup()
    {
        var text = "[muted]";
        var result = Styles.MutedText(text);

        Assert.Contains("[[muted]]", result);
    }

    [Fact]
    public void BoldText_EscapesMarkup()
    {
        var text = "[bold]";
        var result = Styles.BoldText(text);

        Assert.Contains("[[bold]]", result);
    }

    [Fact]
    public void PrimaryText_ContainsColorMarkup()
    {
        var text = "Hello";
        var result = Styles.PrimaryText(text);

        // Should contain opening and closing color tags
        Assert.StartsWith("[", result);
        Assert.EndsWith("[/]", result);
        Assert.Contains("Hello", result);
    }

    [Fact]
    public void BoldText_ContainsBoldMarkup()
    {
        var text = "Hello";
        var result = Styles.BoldText(text);

        Assert.StartsWith("[bold]", result);
        Assert.EndsWith("[/]", result);
        Assert.Contains("Hello", result);
    }

    [Fact]
    public void Colors_AreDefined()
    {
        // Verify all color constants are defined (Colors are value types)
        // Check that they have valid RGB values (not default black)
        Assert.NotEqual(default, Styles.Primary);
        Assert.NotEqual(default, Styles.Secondary);
        Assert.NotEqual(default, Styles.Success);
        Assert.NotEqual(default, Styles.Warning);
        Assert.NotEqual(default, Styles.Error);
        Assert.NotEqual(default, Styles.Muted);
    }

    [Fact]
    public void StyleConstants_AreDefined()
    {
        // Verify all style constants are defined
        Assert.NotNull(Styles.HeaderBorder);
        Assert.NotNull(Styles.Highlight);
        Assert.NotNull(Styles.Active);
        Assert.NotNull(Styles.Disabled);
        Assert.NotNull(Styles.SelectionHighlight);
    }
}
