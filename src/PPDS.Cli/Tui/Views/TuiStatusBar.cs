using PPDS.Cli.Tui.Infrastructure;
using PPDS.Cli.Tui.Testing;
using PPDS.Cli.Tui.Testing.States;
using Terminal.Gui;

namespace PPDS.Cli.Tui.Views;

/// <summary>
/// Interactive status bar with clickable profile and environment sections.
/// </summary>
/// <remarks>
/// <para>
/// The status bar displays the current profile and environment with environment-aware
/// coloring (red for production, yellow for sandbox, green for development).
/// </para>
/// <para>
/// Clicking the profile section opens the profile selector dialog.
/// Clicking the environment section opens the environment selector dialog.
/// </para>
/// <para>
/// This view draws its content directly without child views to ensure reliable
/// mouse event handling. Terminal.Gui child views consume mouse events before
/// the parent can handle them, so we bypass that by not using child views.
/// </para>
/// </remarks>
internal sealed class TuiStatusBar : View, ITuiStateCapture<TuiStatusBarState>
{
    private readonly InteractiveSession _session;
    private readonly ITuiThemeService _themeService;

    // Text content for rendering
    private string _profileText = string.Empty;
    private string _environmentText = string.Empty;

    // Boundary between profile and environment sections (percentage of width)
    private const int ProfileWidthPercent = 40;

    /// <summary>
    /// Event raised when the profile section is clicked.
    /// </summary>
    public event Action? ProfileClicked;

    /// <summary>
    /// Event raised when the environment section is clicked.
    /// </summary>
    public event Action? EnvironmentClicked;

    /// <summary>
    /// Creates a new interactive status bar.
    /// </summary>
    /// <param name="session">The interactive session for state and events.</param>
    public TuiStatusBar(InteractiveSession session)
    {
        _session = session ?? throw new ArgumentNullException(nameof(session));
        _themeService = session.GetThemeService();

        Height = 1;
        Width = Dim.Fill();
        Y = Pos.AnchorEnd(1); // Bottom of screen

        // Enable mouse event reception
        WantMousePositionReports = true;

        // Subscribe to session events
        _session.EnvironmentChanged += OnEnvironmentChanged;
        _session.ProfileChanged += OnProfileChanged;

        // Initial display
        UpdateDisplay();
    }

    /// <summary>
    /// Forces a refresh of the status bar display.
    /// </summary>
    public void Refresh()
    {
        UpdateDisplay();
        SetNeedsDisplay();
    }

    /// <summary>
    /// Handles mouse events for click detection.
    /// </summary>
    public override bool MouseEvent(MouseEvent mouseEvent)
    {
        if (mouseEvent.Flags.HasFlag(MouseFlags.Button1Clicked))
        {
            // Calculate the boundary between profile and environment sections
            var profileWidth = Bounds.Width * ProfileWidthPercent / 100;

            if (mouseEvent.X < profileWidth)
            {
                ProfileClicked?.Invoke();
                return true;
            }
            else
            {
                EnvironmentClicked?.Invoke();
                return true;
            }
        }

        return base.MouseEvent(mouseEvent);
    }

    /// <summary>
    /// Draws the status bar content directly.
    /// </summary>
    public override void Redraw(Rect bounds)
    {
        // Get environment-aware color scheme
        var envType = _themeService.DetectEnvironmentType(_session.CurrentEnvironmentUrl);
        var colorScheme = _themeService.GetStatusBarScheme(envType);

        // Apply the color scheme
        Driver.SetAttribute(colorScheme.Normal);

        // Clear the entire bar
        for (int x = 0; x < bounds.Width; x++)
        {
            Move(x, 0);
            Driver.AddRune(' ');
        }

        // Calculate section widths
        var profileWidth = bounds.Width * ProfileWidthPercent / 100;

        // Draw profile section
        Move(0, 0);
        var profileToRender = _profileText.Length > profileWidth
            ? _profileText[..(profileWidth - 1)] + "\u2026"
            : _profileText;
        Driver.AddStr(profileToRender);

        // Draw environment section
        Move(profileWidth, 0);
        var envWidth = bounds.Width - profileWidth;
        var envToRender = _environmentText.Length > envWidth
            ? _environmentText[..(envWidth - 1)] + "\u2026"
            : _environmentText;
        Driver.AddStr(envToRender);
    }

    private void OnEnvironmentChanged(string? url, string? displayName)
    {
        Application.MainLoop?.Invoke(() =>
        {
            UpdateDisplay();
            SetNeedsDisplay();
        });
    }

    private void OnProfileChanged(string? profileName)
    {
        Application.MainLoop?.Invoke(() =>
        {
            UpdateDisplay();
            SetNeedsDisplay();
        });
    }

    private void UpdateDisplay()
    {
        // Get environment type label
        var envType = _themeService.DetectEnvironmentType(_session.CurrentEnvironmentUrl);
        var envLabel = _themeService.GetEnvironmentLabel(envType);
        var labelSuffix = !string.IsNullOrEmpty(envLabel) ? $" [{envLabel}]" : "";

        // Profile section - show name and identity (e.g., "Josh (josh@contoso.com)")
        var profileName = _session.CurrentProfileName ?? "None";
        var profileIdentity = _session.CurrentProfileIdentity;
        var profileDisplay = !string.IsNullOrEmpty(profileIdentity)
            ? $"{profileName} ({profileIdentity})"
            : profileName;
        profileDisplay = TruncateWithEllipsis(profileDisplay, 18);
        _profileText = $" Profile: {profileDisplay} \u25bc";

        // Environment section
        var envName = _session.CurrentEnvironmentDisplayName ?? "None";
        envName = TruncateWithEllipsis(envName, 28);
        _environmentText = $" Environment: {envName}{labelSuffix} \u25bc";
    }

    /// <summary>
    /// Truncates text to max length with ellipsis if needed.
    /// </summary>
    private static string TruncateWithEllipsis(string text, int maxLength)
    {
        if (string.IsNullOrEmpty(text) || text.Length <= maxLength)
            return text;
        return text[..(maxLength - 1)] + "\u2026"; // Unicode ellipsis
    }

    /// <inheritdoc />
    public TuiStatusBarState CaptureState()
    {
        var envType = _themeService.DetectEnvironmentType(_session.CurrentEnvironmentUrl);
        return new TuiStatusBarState(
            ProfileButtonText: _profileText,
            EnvironmentButtonText: _environmentText,
            EnvironmentType: envType,
            HasProfile: _session.CurrentProfileName != null,
            HasEnvironment: _session.CurrentEnvironmentUrl != null,
            HelpText: null // Status bar doesn't have help text in current implementation
        );
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _session.EnvironmentChanged -= OnEnvironmentChanged;
            _session.ProfileChanged -= OnProfileChanged;
        }
        base.Dispose(disposing);
    }
}
