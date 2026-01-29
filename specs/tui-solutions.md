# TUI Solutions

**Status:** Draft
**Version:** 1.0
**Last Updated:** 2026-01-28
**Code:** None

---

## Overview

The Solutions screen provides an interactive browser for Dataverse solutions and their components. Users can list solutions, inspect component breakdowns, export solutions, and monitor import job progress. It replaces the placeholder "Solutions (Coming Soon)" menu item in TuiShell and follows the DataTableView pattern established by SqlQueryScreen.

### Goals

- **Solution Browsing**: List all solutions with filtering by name, publisher, and managed status
- **Component Inspection**: View solution component breakdown by type (plugins, flows, env vars, etc.)
- **Export Workflow**: Trigger solution export with progress tracking and file output
- **Import Monitoring**: Poll active import jobs and display real-time progress

### Non-Goals

- Solution creation or deletion (use CLI or Power Platform admin center)
- Solution layering or patch management
- Component-level editing (navigate to dedicated screens for plugin registration, flows, etc.)
- Deployment settings generation (use CLI `ppds deploy settings`)

---

## Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                        TuiShell                                  │
│    Tools > Solutions  (replaces "Coming Soon" placeholder)       │
└──────────────────────────────┬──────────────────────────────────┘
                               │
                               ▼
┌─────────────────────────────────────────────────────────────────┐
│                     SolutionScreen                                │
│           (ITuiScreen + ITuiStateCapture)                        │
│                                                                  │
│  ┌──────────────────────────────────────────────────────────┐   │
│  │ DataTableView: Solutions list                              │   │
│  │   Name | UniqueName | Version | Publisher | Managed | Mod  │   │
│  ├──────────────────────────────────────────────────────────┤   │
│  │ Component Panel (on Enter):                                │   │
│  │   Plugin Assemblies: 3  | Flows: 12  | Env Vars: 5        │   │
│  │   Connection Refs: 2   | Web Resources: 45  | ...          │   │
│  ├──────────────────────────────────────────────────────────┤   │
│  │ Status: 24 solutions | Selected: MySolution v1.2.0         │   │
│  └──────────────────────────────────────────────────────────┘   │
└──────────┬──────────────────────────────────────────────────────┘
           │
     ┌─────┴─────┐
     ▼           ▼
┌──────────┐  ┌──────────────────────────────────────────────────┐
│ ISolution │  │ IImportJobService                                │
│ Service   │  │  WaitForCompletionAsync (polling)                │
└──────────┘  └──────────────────────────────────────────────────┘
     │
     ▼
┌─────────────────────────────────────────────────────────────────┐
│              IDataverseConnectionPool                             │
└─────────────────────────────────────────────────────────────────┘
```

### Components

| Component | Responsibility |
|-----------|----------------|
| `SolutionScreen` | Main screen: solution table, component panel, export/import actions |

### Dependencies

- Depends on: [dataverse-services.md](./dataverse-services.md) for `ISolutionService`, `IImportJobService`
- Depends on: [tui.md](./tui.md) for `ITuiScreen`, `IHotkeyRegistry`, `ITuiErrorService`
- Uses patterns from: [architecture.md](./architecture.md) for Application Service boundary

---

## Specification

### Core Requirements

1. Screen implements `ITuiScreen` and `ITuiStateCapture<SolutionScreenState>`
2. Solution list displays in `DataTableView` with columns: Name, UniqueName, Version, Publisher, IsManaged, ModifiedOn
3. Enter on a solution row shows component breakdown in a detail panel below the table
4. Export action triggers `ISolutionService.ExportAsync()` with progress feedback
5. Import monitor shows active import job progress using `IImportJobService.WaitForCompletionAsync()`
6. Filter bar for searching solutions by name
7. All service calls run on background thread; UI updates via `Application.MainLoop.Invoke()`

### Primary Flows

**Browse Solutions:**

1. **Screen opens**: Load solutions via `ISolutionService.ListAsync()`
2. **Filter**: Type in filter bar to search by solution name (debounced)
3. **Toggle managed**: Checkbox or hotkey to include/exclude managed solutions
4. **Select solution**: Arrow keys navigate table; status line shows selected solution details
5. **Refresh**: F5 reloads solution list

**View Solution Components:**

1. **Select solution**: Navigate to solution row
2. **Open components**: Enter loads components via `ISolutionService.GetComponentsAsync(solutionId)`
3. **Display**: Component panel shows grouped counts by component type
4. **Detail table**: Optional second DataTableView listing individual components (name, type, managed)
5. **Navigate**: Future integration point — select a component to navigate to its dedicated screen

**Export Solution:**

1. **Select solution**: Navigate to solution row
2. **Trigger export**: Ctrl+E or menu item
3. **Choose type**: Dialog asks managed/unmanaged export
4. **Export**: `ISolutionService.ExportAsync(uniqueName, managed)` runs on background thread
5. **Save**: File save dialog for output path (defaults to `{uniqueName}_{managed/unmanaged}.zip`)
6. **Progress**: Status line shows "Exporting..." with spinner
7. **Complete**: Status line shows "Exported to {path}" with file size

**Monitor Import:**

1. **Trigger**: Ctrl+I or menu item to check for active imports
2. **List jobs**: `IImportJobService.ListAsync()` shows recent import jobs
3. **Poll active**: If active job found, poll via `WaitForCompletionAsync` with onProgress callback
4. **Display**: Progress bar shows import percentage; status line shows current phase
5. **Complete**: Status line shows final result; refresh solution list to see imported solution

### Constraints

- Solution export can produce files >100MB; status line feedback is essential
- Import monitoring is read-only — this screen does not trigger imports (use CLI or admin center)
- Component counts may be expensive for large solutions; load on demand
- Maximum 500 solutions per query (practical limit for DataTableView)

### Validation Rules

| Field | Rule | Error |
|-------|------|-------|
| Export path | Valid file path with .zip extension | "Export path must end with .zip" |
| Solution name filter | Non-empty string for filter activation | Filter clears on empty string |

---

## Core Types

### SolutionScreen

```csharp
internal sealed class SolutionScreen
    : ITuiScreen, ITuiStateCapture<SolutionScreenState>
{
    public SolutionScreen(
        InteractiveSession session,
        ITuiErrorService errorService,
        Action<DeviceCodeInfo>? deviceCodeCallback = null);

    // ITuiScreen
    public View Content { get; }
    public string Title { get; }  // "Solutions - {environment}"
    public MenuBarItem[]? ScreenMenuItems { get; }
    public Action? ExportAction { get; }

    // ITuiStateCapture
    public SolutionScreenState CaptureState();
}
```

### SolutionScreenState

```csharp
public sealed record SolutionScreenState(
    int SolutionCount,
    string? SelectedSolutionName,
    string? SelectedSolutionVersion,
    bool? SelectedIsManaged,
    int? ComponentCount,
    bool IsLoading,
    bool IsExporting,
    bool IsMonitoringImport,
    double? ImportProgress,
    bool ShowManaged,
    string? FilterText,
    string? ErrorMessage);
```

### Usage Pattern

```csharp
// TuiShell creates and navigates to the screen
var screen = new SolutionScreen(_session, _errorService, _deviceCodeCallback);
NavigateTo(screen);

// Screen internally:
var provider = await _session.GetServiceProviderAsync(_environmentUrl);
var solutionService = provider.GetRequiredService<ISolutionService>();
var solutions = await solutionService.ListAsync();

// Export:
var bytes = await solutionService.ExportAsync("MySolution", managed: false);
File.WriteAllBytes(outputPath, bytes);

// Monitor import:
var importJobService = provider.GetRequiredService<IImportJobService>();
await importJobService.WaitForCompletionAsync(jobId,
    onProgress: progress => Application.MainLoop.Invoke(() =>
    {
        _progressBar.Fraction = progress.Progress / 100.0f;
    }));
```

---

## Error Handling

### Error Types

| Error | Condition | Recovery |
|-------|-----------|----------|
| Service unavailable | No connection to environment | Status line error; F5 to retry |
| Authentication expired | Token expired during export | Re-authentication dialog; retry export |
| Export failed | Solution too large or insufficient privileges | Error dialog with details |
| Import timeout | Import job exceeds 30-minute timeout | Status line warning; manual check suggested |
| Solution not found | Solution deleted between list and export | Refresh list; show warning |

### Recovery Strategies

- **Connection errors**: Status line error with F5 retry hint
- **Auth errors**: `ReAuthenticationDialog` → retry pending operation
- **Export errors**: Full error dialog with Dataverse error details
- **Import stall**: Option to stop monitoring; suggest checking admin center

### Edge Cases

| Scenario | Expected Behavior |
|----------|-------------------|
| No solutions in environment | Empty table with "No solutions found" message |
| Solution with 0 components | Component panel shows "No components" |
| Very large solution (>100MB) | Export shows progress; no timeout on export itself |
| Multiple active imports | Show most recent import; list view for all |
| Managed solution selected | Export button still available; unmanaged export option hidden |

---

## Design Decisions

### Why DataTableView Instead of TreeView?

**Context:** Solutions contain components, which could be shown as a tree (Solution → Component Type → Components). However, the primary action is browsing solutions, not drilling into component hierarchies.

**Decision:** Use DataTableView for solutions list with an expandable component panel below. Component drill-down navigates to dedicated screens.

**Alternatives considered:**
- TreeView (Solution → Components): Rejected — solution list is the primary view; tree adds unnecessary depth
- Separate component screen: Possible but adds navigation friction for a quick overview

**Consequences:**
- Positive: Familiar table UI, consistent with SqlQueryScreen
- Positive: Component panel provides overview without losing solution list context
- Negative: Cannot see multiple solutions' components simultaneously

### Why Read-Only Import Monitoring?

**Context:** Import could be triggered from this screen, but solution import requires a .zip file and has complex options (overwrite, publish workflows, etc.).

**Decision:** Monitor-only. Import is triggered via CLI (`ppds solutions import`) or admin center. The TUI shows progress of active imports.

**Alternatives considered:**
- Full import workflow with file picker: Rejected — complex UX for an infrequent operation
- No import monitoring: Rejected — users want to see import progress in-context

**Consequences:**
- Positive: Simpler screen; import complexity stays in CLI
- Positive: Still provides value for monitoring long-running imports
- Negative: Users must use CLI or admin center to start imports

---

## Configuration

| Setting | Type | Default | Description |
|---------|------|---------|-------------|
| ShowManaged | bool | true | Include managed solutions in list |
| ImportPollInterval | TimeSpan | 5s | Import job polling interval |

---

## Testing

### Acceptance Criteria

- [ ] Screen loads solutions from ISolutionService.ListAsync on activation
- [ ] Filter bar searches solutions by name (debounced)
- [ ] Managed toggle includes/excludes managed solutions
- [ ] Enter on solution loads component breakdown via GetComponentsAsync
- [ ] Ctrl+E triggers export with managed/unmanaged choice
- [ ] Export saves .zip file to selected path
- [ ] Import monitor polls active jobs with progress callback
- [ ] Status line shows solution count and selected solution info
- [ ] State capture returns accurate SolutionScreenState
- [ ] F5 refreshes solution list

### Edge Cases

| Scenario | Input | Expected Output |
|----------|-------|-----------------|
| Empty environment | No solutions | Empty table, status "0 solutions" |
| Filter matches none | Restrictive filter text | Empty table, status "0 solutions matching filter" |
| Export cancelled | User cancels file dialog | No export, return to table |
| No active imports | Ctrl+I with no jobs | "No active import jobs" message |
| Large solution export | Solution >50MB | Progress spinner, no timeout |

### Test Examples

```csharp
[Fact]
[Trait("Category", "TuiUnit")]
public void SolutionScreen_CapturesInitialState()
{
    var session = CreateMockSession();
    var screen = new SolutionScreen(session, new TuiErrorService());

    var state = screen.CaptureState();

    Assert.Equal(0, state.SolutionCount);
    Assert.Null(state.SelectedSolutionName);
    Assert.False(state.IsLoading);
    Assert.False(state.IsExporting);
    Assert.True(state.ShowManaged);
}

[Fact]
[Trait("Category", "TuiUnit")]
public void SolutionScreen_CapturesSelectedSolution()
{
    var session = CreateMockSessionWithSolutions(
        new SolutionInfo { UniqueName = "MySolution", Version = "1.2.0" });
    var screen = new SolutionScreen(session, new TuiErrorService());
    screen.SelectSolution(0);

    var state = screen.CaptureState();

    Assert.Equal("MySolution", state.SelectedSolutionName);
    Assert.Equal("1.2.0", state.SelectedSolutionVersion);
}
```

---

## Related Specs

- [dataverse-services.md](./dataverse-services.md) - Backend: ISolutionService, IImportJobService
- [tui.md](./tui.md) - TUI framework: ITuiScreen, DataTableView, state capture
- [tui-plugin-registration.md](./tui-plugin-registration.md) - Navigate from solution component to plugin registrations
- [tui-environment-dashboard.md](./tui-environment-dashboard.md) - Navigate from solution component to env vars, flows, etc.
- [architecture.md](./architecture.md) - Application Service boundary pattern

---

## Roadmap

- Component-level navigation: select a plugin assembly → open PluginRegistrationScreen filtered to it
- Solution comparison: diff two solutions' component lists
- Solution publisher management
- Solution history timeline
