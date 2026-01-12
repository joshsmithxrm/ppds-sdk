using PPDS.Cli.Tui.Infrastructure;
using Terminal.Gui;

namespace PPDS.Cli.Tui.Views;

/// <summary>
/// Status line for displaying contextual messages and operation feedback.
/// </summary>
/// <remarks>
/// <para>
/// TuiStatusLine provides a consistent bottom-line status area that supports
/// both static messages and animated spinner for long operations.
/// </para>
/// <para>
/// Use as part of the standard 2-line footer pattern:
/// - Line 1: TuiStatusLine (contextual status) - above
/// - Line 2: TuiStatusBar (profile/environment buttons) - bottom
/// </para>
/// </remarks>
internal sealed class TuiStatusLine : View
{
    /// <summary>
    /// Braille spinner animation frames (Unicode Braille Pattern Dots).
    /// </summary>
    private static readonly string[] SpinnerFrames = { "\u280b", "\u2819", "\u2839", "\u2838", "\u283c", "\u2834", "\u2826", "\u2827", "\u2807", "\u280f" };

    private const int AnimationIntervalMs = 100;

    private int _frameIndex;
    private object? _timer;
    private string _message = string.Empty;
    private bool _isSpinning;

    /// <summary>
    /// Creates a new status line positioned at the bottom of the screen.
    /// </summary>
    public TuiStatusLine()
    {
        Height = 1;
        Width = Dim.Fill();
        Y = Pos.AnchorEnd(2); // Above TuiStatusBar
        ColorScheme = TuiColorPalette.Default;
    }

    /// <summary>
    /// Sets a static status message (no spinner).
    /// </summary>
    /// <param name="message">The message to display, or null/empty to clear.</param>
    public void SetMessage(string? message)
    {
        StopSpinnerInternal();
        _message = message ?? string.Empty;
        UpdateDisplay();
    }

    /// <summary>
    /// Clears the status message.
    /// </summary>
    public void ClearMessage()
    {
        StopSpinnerInternal();
        _message = string.Empty;
        UpdateDisplay();
    }

    /// <summary>
    /// Shows an animated spinner with a message.
    /// </summary>
    /// <param name="message">The message to display next to the spinner.</param>
    public void ShowSpinner(string message)
    {
        _message = message ?? string.Empty;

        if (_isSpinning)
        {
            // Already spinning, just update message
            UpdateDisplay();
            return;
        }

        _isSpinning = true;
        _frameIndex = 0;

        UpdateDisplay();

        // Start animation timer
        _timer = Application.MainLoop?.AddTimeout(
            TimeSpan.FromMilliseconds(AnimationIntervalMs),
            OnTimerTick);
    }

    /// <summary>
    /// Stops the spinner and optionally shows a final message.
    /// </summary>
    /// <param name="message">Optional final message to display.</param>
    public void HideSpinner(string? message = null)
    {
        StopSpinnerInternal();
        if (message != null)
        {
            _message = message;
        }
        UpdateDisplay();
    }

    /// <summary>
    /// Gets whether the spinner is currently animating.
    /// </summary>
    public bool IsSpinning => _isSpinning;

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            StopSpinnerInternal();
        }
        base.Dispose(disposing);
    }

    private void StopSpinnerInternal()
    {
        _isSpinning = false;

        if (_timer != null)
        {
            Application.MainLoop?.RemoveTimeout(_timer);
            _timer = null;
        }
    }

    private bool OnTimerTick(MainLoop mainLoop)
    {
        if (!_isSpinning)
        {
            return false; // Stop the timer
        }

        _frameIndex = (_frameIndex + 1) % SpinnerFrames.Length;
        UpdateDisplay();

        return true; // Continue the timer
    }

    private void UpdateDisplay()
    {
        Application.MainLoop?.Invoke(() =>
        {
            ClearView();

            string displayText;
            if (_isSpinning)
            {
                var spinnerChar = SpinnerFrames[_frameIndex];
                displayText = string.IsNullOrEmpty(_message)
                    ? spinnerChar
                    : $"{spinnerChar} {_message}";
            }
            else
            {
                displayText = _message;
            }

            if (!string.IsNullOrEmpty(displayText))
            {
                Move(0, 0);
                Driver?.AddStr($" {displayText}");
            }

            SetNeedsDisplay();
        });
    }

    private void ClearView()
    {
        // Fill the line with spaces to clear previous content
        Move(0, 0);
        var width = Bounds.Width;
        if (width > 0)
        {
            Driver?.AddStr(new string(' ', width));
        }
    }
}
