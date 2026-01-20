# PPDS.TUI: Architecture

## Overview

The TUI (Terminal User Interface) subsystem provides an interactive console-based interface for PPDS, built on Terminal.Gui. The architecture follows a shell-and-screen pattern where `TuiShell` provides persistent navigation infrastructure (menu bar, status bar, hotkey handling) while `ITuiScreen` implementations provide workspace-specific functionality. This design enables a modern development experience with connection pooling, environment-aware theming, and comprehensive keyboard navigation.

## Public API

### Interfaces

| Interface | Purpose |
|-----------|---------|
| `ITuiScreen` | Contract for screens hosted in the shell (content, menus, lifecycle) |
| `ITuiStateCapture<TState>` | State capture for autonomous testing without visual inspection |
| `IHotkeyRegistry` | Context-aware keyboard shortcut management |
| `ITuiThemeService` | Environment detection and color scheme selection |
| `ITuiErrorService` | Centralized error handling and display |

### Classes

| Class | Purpose |
|-------|---------|
| `PpdsApplication` | Entry point - initializes Terminal.Gui and runs the shell |
| `TuiShell` | Main window with menu bar, status bar, screen navigation |
| `InteractiveSession` | Connection pool lifecycle and service provider management |
| `TuiDialog` | Base class for dialogs with standardized behavior |
| `HotkeyRegistry` | Implementation of context-aware hotkey routing |
| `TuiColorPalette` | Centralized color scheme definitions |
| `TuiDebugLog` | Session debug logging for diagnostics |

### DTOs/Models

| Type | Purpose |
|------|---------|
| `HotkeyBinding` | Registered hotkey with key, scope, handler, and description |
| `HotkeyScope` | Scope enum (Global, Screen, Dialog) |
| `EnvironmentType` | Environment classification (Production, Sandbox, Development, Trial) |
| `TuiError` | Error record with summary, details, and context |
| `*State` records | State capture types for each component (e.g., `TuiShellState`) |

## Behaviors

### Shell-and-Screen Architecture

```
PpdsApplication
    │
    └── TuiShell (Window)
            │
            ├── MenuBar (persistent)
            ├── ContentArea (FrameView)
            │       │
            │       └── ITuiScreen.Content (swappable)
            │
            ├── TuiStatusLine (messages)
            └── TuiStatusBar (profile/environment)
```

### Screen Navigation

| Method | Behavior |
|--------|----------|
| `NavigateTo(screen)` | Pushes current screen to stack, activates new screen |
| `NavigateBack()` | Disposes current screen, restores previous from stack |
| Screen stack empty | Shows main menu content |

### Lifecycle

1. **Initialization**: `PpdsApplication.Run()` initializes Terminal.Gui, creates `InteractiveSession`
2. **Warm-up**: Connection pool initialization starts in background during TUI startup
3. **Operation**: User navigates screens, runs queries, switches environments
4. **Shutdown**: `Application.Shutdown()`, session disposal with timeout protection

### Connection Pool Management (InteractiveSession)

| Operation | Behavior |
|-----------|----------|
| First query | Creates `ServiceProvider` with connection pool |
| Subsequent queries | Reuses existing pool |
| Environment change | Disposes old pool, creates new on next use |
| Profile switch | Invalidates pool, re-warms with new credentials |
| Session end | Disposes pool with timeout (prevents hang on exit) |

### Hotkey Scoping

Priority order (highest to lowest): Dialog → Screen → Global

| Scope | When Active | Use Case |
|-------|-------------|----------|
| Global | Always | Alt+P (profile), Alt+E (environment), F1 (help), F12 (errors) |
| Screen | When screen is focused (no dialog) | Ctrl+Enter (execute query) |
| Dialog | When specific dialog is open | Dialog-specific shortcuts |

### Menu Bar Structure

| Menu | Contents |
|------|----------|
| _File | Export, Exit |
| Screen-specific | Inserted between File and Tools |
| _Tools | SQL Query, Data Migration, Solutions, Plugin Traces |
| _Help | About, Keyboard Shortcuts, Error Log |

### Environment-Aware Theming

Status bar color changes based on detected environment type:

| Environment | Color Scheme |
|-------------|--------------|
| Production | White on Red (danger) |
| Sandbox | Black on Yellow (warning) |
| Development | White on Green (safe) |
| Trial | Black on Cyan (info) |
| Unknown | Black on Gray (neutral) |

## Edge Cases

| Scenario | Behavior | Notes |
|----------|----------|-------|
| No profile configured | Shows profile selector | Cannot proceed without profile |
| Auth token expired (401) | Offers re-authentication dialog | `InvalidateAndReauthenticateAsync` |
| SPN auth before TUI init | Falls back to browser auth | MainLoop not available for dialog |
| Global hotkey with dialog open | Closes dialog, does not execute | Press again after dialog closes |
| Menu click debounce | 150ms delay prevents double-clicks | Prevents Terminal.Gui flicker |
| Session disposal timeout | 2-3 seconds max | Prevents hang on ServiceClient.Dispose |
| Console cursor | Block cursor set via ANSI escape | Better visibility in text fields |

## Error Handling

### Error Flow

```
Exception → ITuiErrorService.ReportError() → TuiError record → ErrorOccurred event
    │
    └── TuiShell → Status line message → F12 shows ErrorDetailsDialog
```

### Error Display

| Component | Role |
|-----------|------|
| `TuiStatusLine` | Brief error message with "F12 for details" |
| `ErrorDetailsDialog` | Full error details and debug log |
| `TuiDebugLog` | Session-scoped debug entries (cleared on startup) |

## Dependencies

- **Internal**:
  - `PPDS.Auth` - Profile storage, credential providers
  - `PPDS.Dataverse` - Connection pooling
  - `PPDS.Cli.Services` - Application services (SQL query, export, etc.)
- **External**:
  - `Terminal.Gui` (1.19+) - TUI framework
  - `Microsoft.Extensions.DependencyInjection` - Service resolution

## Configuration

### PpdsApplication Constructor

| Parameter | Type | Description |
|-----------|------|-------------|
| `profileName` | `string?` | Profile to use (null for active profile) |
| `deviceCodeCallback` | `Action<DeviceCodeInfo>?` | Callback for device code display |

### InteractiveSession Constructor

| Parameter | Type | Description |
|-----------|------|-------------|
| `profileName` | `string?` | Profile filter |
| `profileStore` | `ProfileStore` | Shared profile store |
| `serviceProviderFactory` | `IServiceProviderFactory?` | DI factory (null for default) |
| `deviceCodeCallback` | `Action<DeviceCodeInfo>?` | Device code display |
| `beforeInteractiveAuth` | `Func<..., PreAuthDialogResult>?` | Pre-auth dialog callback |

### Color Palette Design Rules

| Rule | Description |
|------|-------------|
| Blue background rule | When bg is Cyan/BrightCyan/Blue/BrightBlue, fg MUST be Black |
| Dark theme | Black background, white/cyan text for readability |
| Focus indication | Cyan background with black text |
| Disabled state | DarkGray text |

## Thread Safety

### Terminal.Gui Threading Model

- Main loop runs on single UI thread
- Background operations must marshal to UI via `Application.MainLoop.Invoke()`
- `InteractiveSession` uses `SemaphoreSlim` for service provider access
- Global hotkey handlers are serialized (one at a time)

### Async Patterns

| Pattern | Usage |
|---------|-------|
| Fire-and-forget | Pool warming, profile loading (with error handling via ContinueWith) |
| Marshal to UI | `Application.MainLoop.Invoke()` for state updates |
| Sync-over-async | Required for Terminal.Gui entry/exit (documented exception) |

## Testing Strategy (ADR-0028)

### State Capture Pattern

Components implement `ITuiStateCapture<TState>` for autonomous testing:

```csharp
public interface ITuiStateCapture<out TState>
{
    TState CaptureState();
}
```

### State Record Types

| Component | State Record |
|-----------|--------------|
| `TuiShell` | `TuiShellState` |
| `TuiStatusBar` | `TuiStatusBarState` |
| `SqlQueryScreen` | `SqlQueryScreenState` |
| Each dialog | `*DialogState` |

### Test Categories

| Category | Filter | Purpose |
|----------|--------|---------|
| TuiUnit | `Category=TuiUnit` | Logic tests without Terminal.Gui |
| Contract tests | N/A | Interface compliance verification |

## Related

- [PPDS.Cli Services: Application Services](../04-cli-services/01-application-services.md) - Service layer consumed by TUI
- [ADR-0028: TUI Testing Strategy](../../docs/adr/0028_TUI_TESTING_STRATEGY.md) - Testing strategy
- [ADR-0015: Application Service Layer](../../docs/adr/0015_APPLICATION_SERVICE_LAYER.md) - Architecture principles

## Source Files

| File | Purpose |
|------|---------|
| `src/PPDS.Cli/Tui/PpdsApplication.cs` | Entry point, Terminal.Gui lifecycle |
| `src/PPDS.Cli/Tui/TuiShell.cs` | Main shell with navigation |
| `src/PPDS.Cli/Tui/InteractiveSession.cs` | Connection pool and session management |
| `src/PPDS.Cli/Tui/Screens/ITuiScreen.cs` | Screen contract |
| `src/PPDS.Cli/Tui/Screens/SqlQueryScreen.cs` | SQL query workspace |
| `src/PPDS.Cli/Tui/Dialogs/TuiDialog.cs` | Base dialog class |
| `src/PPDS.Cli/Tui/Infrastructure/HotkeyRegistry.cs` | Keyboard shortcut management |
| `src/PPDS.Cli/Tui/Infrastructure/TuiColorPalette.cs` | Color scheme definitions |
| `src/PPDS.Cli/Tui/Infrastructure/ITuiErrorService.cs` | Error handling interface |
| `src/PPDS.Cli/Tui/Infrastructure/TuiErrorService.cs` | Error handling implementation |
| `src/PPDS.Cli/Tui/Infrastructure/TuiDebugLog.cs` | Debug logging |
| `src/PPDS.Cli/Tui/Testing/ITuiStateCapture.cs` | State capture interface |
| `src/PPDS.Cli/Tui/Testing/States/` | State record types for testing |
| `tests/PPDS.Cli.Tests/Tui/` | TUI unit tests |
