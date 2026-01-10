# TUI Loading Pattern

Pattern for providing animated feedback during async operations in the TUI.

## When to Use

Use animated spinners for any async operation expected to take more than ~500ms:

| Operation | Use Spinner |
|-----------|-------------|
| Environment discovery | Yes |
| Profile loading | Yes |
| Query execution | Yes |
| Auth flows (device code) | Yes |
| Quick file operations | No |
| Simple data lookups | No |

## TuiSpinner Component

`TuiSpinner` provides animated braille spinner feedback:

```csharp
using PPDS.Cli.Tui.Views;

// Create and position the spinner
var spinner = new TuiSpinner
{
    X = 1,
    Y = 1,
    Width = Dim.Fill() - 2,
    Height = 1
};
Add(spinner);

// Start with a message
spinner.Start("Loading environments...");

// ... async operation ...

// Stop and hide when done
spinner.Stop();

// Or stop with a final message
spinner.StopWithMessage("Found 5 environments");
```

### Animation

The spinner uses Unicode braille characters that cycle at 100ms intervals:

```
⠋ ⠙ ⠹ ⠸ ⠼ ⠴ ⠦ ⠧ ⠇ ⠏
```

This provides smooth animation without being distracting.

## Integration Patterns

### Pattern 1: Spinner with Status Label

For dialogs where you want the spinner to replace a status label during loading:

```csharp
// Spinner shown during loading
_spinner = new TuiSpinner
{
    X = 1,
    Y = Pos.Bottom(listFrame),
    Width = Dim.Fill() - 2,
    Height = 1
};

// Status label shown after loading
_statusLabel = new Label(string.Empty)
{
    X = 1,
    Y = Pos.Bottom(listFrame),
    Width = Dim.Fill() - 2,
    Height = 1,
    Visible = false  // Hidden initially
};

Add(_spinner, _statusLabel);

// Start loading
_spinner.Start("Loading...");

// After loading completes:
_spinner.Stop();
_statusLabel.Text = "Found 10 items";
_statusLabel.Visible = true;
```

### Pattern 2: TuiOperationProgress with Spinner

For operations using the `IOperationProgress` interface (ADR-0025):

```csharp
var spinner = new TuiSpinner { ... };
var progress = new TuiOperationProgress(
    progressBar: null,
    statusLabel: null,
    spinner: spinner);

// Now ReportStatus() animates the spinner
progress.ReportStatus("Processing...");

// ReportComplete() stops spinner and shows final message
progress.ReportComplete("Done!");
```

### Pattern 3: Inline Status Spinner

For screens where the spinner replaces the status bar temporarily:

```csharp
private void UpdateStatus(string message, bool showSpinner = false)
{
    Application.MainLoop?.Invoke(() =>
    {
        if (showSpinner)
        {
            _statusLabel.Visible = false;
            _statusSpinner.Start(message);
        }
        else
        {
            _statusSpinner.Stop();
            _statusLabel.Text = message;
            _statusLabel.Visible = true;
        }
    });
}

// Usage:
UpdateStatus("Executing query...", showSpinner: true);
// ... operation ...
UpdateStatus("Returned 100 rows", showSpinner: false);
```

## Thread Safety

The `TuiSpinner` handles thread safety internally:
- Timer callbacks run on the main loop
- All UI updates use `Application.MainLoop?.Invoke()`
- Safe to call `Start()` and `Stop()` from any thread

## Best Practices

1. **Always stop the spinner** - Use try/finally or ensure all code paths stop the spinner
2. **Meaningful messages** - Use action verbs: "Loading...", "Connecting...", "Executing..."
3. **Handle errors** - Stop spinner and show error state
4. **Avoid nested spinners** - One spinner per visible area

## Examples in Codebase

- `EnvironmentSelectorDialog.cs` - Spinner during environment discovery
- `ProfileSelectorDialog.cs` - Spinner during profile loading
- `SqlQueryScreen.cs` - Spinner during query execution
- `DeviceCodeAuthDialog.cs` - ASCII spinner during auth polling (predates TuiSpinner)

## Related

- ADR-0025: UI-Agnostic Progress Reporting
- `TuiOperationProgress.cs` - Progress adapter with spinner support
