using PPDS.Cli.Tui.Infrastructure;
using Xunit;

namespace PPDS.Cli.Tests.Tui.Infrastructure;

/// <summary>
/// Unit tests for <see cref="TuiColorPalette"/>.
/// </summary>
/// <remarks>
/// These tests verify color scheme definitions follow the blue background rule.
/// Tests can run without Terminal.Gui context since ColorScheme creation uses fallback.
/// </remarks>
public class TuiColorPaletteTests
{
    /// <summary>
    /// Validates that all color schemes follow the blue background rule:
    /// When background is Cyan, BrightCyan, Blue, or BrightBlue, foreground MUST be Black.
    /// </summary>
    [Fact]
    public void AllColorSchemes_BlueBackgrounds_MustHaveBlackForeground()
    {
        var violations = TuiColorPalette.ValidateBlueBackgroundRule().ToList();

        if (violations.Count > 0)
        {
            var message = string.Join(Environment.NewLine, violations.Select(v =>
                $"  {v.Scheme}.{v.Attribute}: {v.Foreground} on {v.Background} (should be Black on {v.Background})"));

            Assert.Fail($"Blue background rule violations found:{Environment.NewLine}{message}");
        }
    }

    /// <summary>
    /// Ensures all color schemes are accessible without Terminal.Gui initialization.
    /// </summary>
    [Fact]
    public void AllColorSchemes_AccessWithoutGuiInit_DoesNotThrow()
    {
        // These should not throw even without Terminal.Gui Application.Init()
        var schemes = new[]
        {
            TuiColorPalette.Default,
            TuiColorPalette.Focused,
            TuiColorPalette.TextInput,
            TuiColorPalette.ReadOnlyText,
            TuiColorPalette.FileDialog,
            TuiColorPalette.StatusBar_Production,
            TuiColorPalette.StatusBar_Sandbox,
            TuiColorPalette.StatusBar_Development,
            TuiColorPalette.StatusBar_Trial,
            TuiColorPalette.StatusBar_Default,
            TuiColorPalette.MenuBar,
            TuiColorPalette.TableHeader,
            TuiColorPalette.Selected,
            TuiColorPalette.Error,
            TuiColorPalette.Success
        };

        foreach (var scheme in schemes)
        {
            Assert.NotNull(scheme);
        }
    }
}
