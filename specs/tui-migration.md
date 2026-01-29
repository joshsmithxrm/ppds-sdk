# TUI Data Migration

**Status:** Draft
**Version:** 1.0
**Last Updated:** 2026-01-28
**Code:** None

---

## Overview

The Data Migration screen provides an interactive interface for configuring and monitoring CMT-format data export and import operations. It replaces the placeholder "Data Migration (Coming Soon)" menu item in TuiShell. Migration operations are long-running (minutes to hours) where real-time progress tracking, phase visibility, and error inspection add significant value over CLI scrolling output.

### Goals

- **Configuration UI**: Configure export (schema file, output path, parallelism) and import (data file, options) with visual forms
- **Real-Time Progress**: Phase indicator, entity-level progress bars, rate/ETA display adapted from IProgressReporter
- **Execution Plan Preview**: Visualize dependency tiers, deferred fields, and M2M relationships before starting import
- **Error Inspection**: Post-operation drill-down into per-entity success/failure counts and individual error details

### Non-Goals

- Schema file creation or editing (use CLI `ppds data schema generate`)
- Direct record editing during migration
- Cross-environment migration orchestration (use CLI or CI/CD)
- Solution export/import (handled by [tui-solutions.md](./tui-solutions.md))

---

## Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                        TuiShell                                  │
│  Tools > Data Migration  (replaces "Coming Soon" placeholder)    │
└──────────────────────────────┬──────────────────────────────────┘
                               │
                               ▼
┌─────────────────────────────────────────────────────────────────┐
│                     MigrationScreen                               │
│           (ITuiScreen + ITuiStateCapture)                        │
│                                                                  │
│  ┌──────────────────────────────────────────────────────────┐   │
│  │ Mode: [Export] [Import]    Schema: /path/schema.xml       │   │
│  ├──────────────────────────────────────────────────────────┤   │
│  │ Progress:                                                  │   │
│  │  Phase: Entity Import (2/3)   Entity: account (450/1200)  │   │
│  │  ████████████░░░░░░░░░  37%   Rate: 120 rec/s  ETA: 4m   │   │
│  │                                                            │   │
│  │  Entity Progress:                                          │   │
│  │  ✓ businessunit  50/50    ✓ team  25/25                    │   │
│  │  ▸ account       450/1200  · contact  0/800               │   │
│  ├──────────────────────────────────────────────────────────┤   │
│  │ Results / Errors:                                          │   │
│  │   account: 1200 ok, 3 errors  |  contact: 800 ok, 0 err  │   │
│  ├──────────────────────────────────────────────────────────┤   │
│  │ Status: Importing... Phase 1 of 3 | Elapsed: 2m 15s       │   │
│  └──────────────────────────────────────────────────────────┘   │
└──────────┬──────────────────────────────────────────────────────┘
           │
           ▼
┌──────────────────────────┐
│ ExecutionPlanPreview     │
│ Dialog                   │
│ Tier 0: businessunit,    │
│         team             │
│ Tier 1: account          │
│ Tier 2: contact          │
│ Deferred: account.       │
│   parentaccountid        │
│ M2M: systemuserroles     │
└──────────────────────────┘
           │
           ▼
┌─────────────────────────────────────────────────────────────────┐
│            IExporter / IImporter                                  │
│  IDependencyGraphBuilder / IExecutionPlanBuilder                 │
│  IProgressReporter                                               │
└──────────────────────────────┬──────────────────────────────────┘
                               │
                               ▼
┌─────────────────────────────────────────────────────────────────┐
│      IDataverseConnectionPool / IBulkOperationExecutor           │
└─────────────────────────────────────────────────────────────────┘
```

### Components

| Component | Responsibility |
|-----------|----------------|
| `MigrationScreen` | Main screen: mode selection, configuration, progress display, results |
| `ExecutionPlanPreviewDialog` | Modal: preview tier ordering, deferred fields, M2M before import |
| `TuiMigrationProgressReporter` | Adapts IProgressReporter to TUI progress views |

### Dependencies

- Depends on: [migration.md](./migration.md) for `IExporter`, `IImporter`, `IDependencyGraphBuilder`, `IExecutionPlanBuilder`, all models
- Depends on: [bulk-operations.md](./bulk-operations.md) consumed internally by importer
- Depends on: [tui.md](./tui.md) for `ITuiScreen`, `TuiDialog`, `IHotkeyRegistry`, `ITuiErrorService`
- Uses patterns from: [architecture.md](./architecture.md) for Application Service boundary and progress reporting

---

## Specification

### Core Requirements

1. Screen implements `ITuiScreen` and `ITuiStateCapture<MigrationScreenState>`
2. Two modes: Export and Import, selected via RadioGroup at top
3. Configuration panel provides file paths and option settings per mode
4. Progress area shows phase, entity-level progress, rate, and ETA
5. Results area shows per-entity success/failure counts after completion
6. Execution plan preview dialog visualizes import ordering before starting
7. `TuiMigrationProgressReporter` implements `IProgressReporter` and marshals updates to UI thread
8. All migration operations run on background thread; UI updates via `Application.MainLoop.Invoke()`
9. Cancel button sends `CancellationToken` cancellation during operation

### Primary Flows

**Export Flow:**

1. **Select Export mode**: RadioGroup at top switches to export configuration
2. **Configure**: Set schema file path (TextField + file browser), output path, parallelism (slider/spinner)
3. **Start export**: Ctrl+Enter or Start button launches `IExporter.ExportAsync()` on background thread
4. **Progress**: Phase label updates ("Exporting entity: account"), entity-level progress with record counts
5. **Complete**: Results area shows per-entity record counts, total time elapsed
6. **Error**: If export fails, error details shown in results area; status line shows error summary

**Import Flow:**

1. **Select Import mode**: RadioGroup at top switches to import configuration
2. **Configure**: Set data file path, import options (mode, bulk APIs, plugin bypass, continue-on-error, etc.)
3. **Preview plan**: Ctrl+P opens `ExecutionPlanPreviewDialog`:
   - Reads data via `ICmtDataReader`
   - Builds dependency graph via `IDependencyGraphBuilder`
   - Builds execution plan via `IExecutionPlanBuilder`
   - Shows tiers, deferred fields, M2M relationships
4. **Start import**: Ctrl+Enter launches `IImporter.ImportAsync()` on background thread
5. **Progress**: Three-phase display:
   - Phase 1 (Entity Import): Per-tier progress, per-entity within tier, record rate/ETA
   - Phase 2 (Deferred Fields): Entity-level deferred update progress
   - Phase 3 (M2M Relationships): Relationship-level progress
6. **Complete**: Results area shows per-entity success/failure/warning counts
7. **Inspect errors**: Select entity in results → see individual error records with details

**Cancel Operation:**

1. **During operation**: Cancel button enabled; Escape also cancels with confirmation
2. **Cancel**: Fires CancellationToken; operation completes current batch then stops
3. **Status**: Shows "Cancelled" with partial results displayed

### Constraints

- Only one operation (export or import) can run at a time
- File paths must be validated before starting (file exists for import, directory exists for export)
- Import options cannot be changed while import is running
- Progress updates from IProgressReporter arrive on background threads; must marshal to UI
- Large imports may run for hours; screen must remain responsive throughout
- Export/import operations are not resumable (cancelled = start over)

### Validation Rules

| Field | Rule | Error |
|-------|------|-------|
| Schema path (export) | File must exist with .xml extension | "Schema file not found" |
| Output path (export) | Directory must exist | "Output directory not found" |
| Data path (import) | File must exist with .zip extension | "Data file not found" |
| Max parallel entities | 1 ≤ value ≤ 16 | "Parallelism must be between 1 and 16" |
| Import mode | Valid ImportMode enum | "Invalid import mode" |

---

## Core Types

### MigrationScreen

```csharp
internal sealed class MigrationScreen
    : ITuiScreen, ITuiStateCapture<MigrationScreenState>
{
    public MigrationScreen(
        InteractiveSession session,
        ITuiErrorService errorService,
        Action<DeviceCodeInfo>? deviceCodeCallback = null);

    // ITuiScreen
    public View Content { get; }
    public string Title { get; }  // "Data Migration - {environment}"
    public MenuBarItem[]? ScreenMenuItems { get; }
    public Action? ExportAction { get; }

    // ITuiStateCapture
    public MigrationScreenState CaptureState();
}
```

### MigrationScreenState

```csharp
public sealed record MigrationScreenState(
    MigrationMode Mode,
    MigrationOperationState OperationState,
    string? SchemaPath,
    string? DataPath,
    string? OutputPath,
    string? CurrentPhase,
    string? CurrentEntity,
    int EntitiesProcessed,
    int EntitiesTotal,
    int RecordsProcessed,
    int RecordsTotal,
    double? ProgressPercent,
    double? RecordsPerSecond,
    TimeSpan? Elapsed,
    TimeSpan? EstimatedRemaining,
    int ErrorCount,
    int WarningCount,
    string? ErrorMessage);
```

### MigrationMode

```csharp
public enum MigrationMode
{
    Export,
    Import
}
```

### MigrationOperationState

```csharp
public enum MigrationOperationState
{
    Idle,
    Configuring,
    PreviewingPlan,
    Running,
    Completed,
    Failed,
    Cancelled
}
```

### TuiMigrationProgressReporter

Adapts `IProgressReporter` to TUI progress views. Marshals all callbacks to the UI thread.

```csharp
internal sealed class TuiMigrationProgressReporter : IProgressReporter
{
    public TuiMigrationProgressReporter(
        Action<ProgressEventArgs> onProgress,
        Action<MigrationResult> onComplete,
        Action<Exception, string?> onError);

    public string OperationName { get; set; }

    public void Report(ProgressEventArgs args)
    {
        Application.MainLoop.Invoke(() => _onProgress(args));
    }

    public void Complete(MigrationResult result)
    {
        Application.MainLoop.Invoke(() => _onComplete(result));
    }

    public void Error(Exception ex, string? context)
    {
        Application.MainLoop.Invoke(() => _onError(ex, context));
    }

    public void Reset() { /* Reset UI state */ }
}
```

### ExecutionPlanPreviewDialog

Modal dialog showing the import execution plan before starting.

```csharp
internal sealed class ExecutionPlanPreviewDialog
    : TuiDialog, ITuiStateCapture<ExecutionPlanPreviewDialogState>
{
    public ExecutionPlanPreviewDialog(
        ExecutionPlan plan,
        InteractiveSession? session = null);

    public bool IsApproved { get; }

    public ExecutionPlanPreviewDialogState CaptureState();
}
```

### ExecutionPlanPreviewDialogState

```csharp
public sealed record ExecutionPlanPreviewDialogState(
    int TierCount,
    int EntityCount,
    int DeferredFieldCount,
    int M2MRelationshipCount,
    bool IsApproved,
    IReadOnlyList<string> TierSummaries);
```

### Usage Pattern

```csharp
// TuiShell creates and navigates to the screen
var screen = new MigrationScreen(_session, _errorService, _deviceCodeCallback);
NavigateTo(screen);

// Export:
var provider = await _session.GetServiceProviderAsync(_environmentUrl);
var exporter = provider.GetRequiredService<IExporter>();
var progressReporter = new TuiMigrationProgressReporter(
    onProgress: args => UpdateProgressUI(args),
    onComplete: result => ShowResults(result),
    onError: (ex, ctx) => ShowError(ex, ctx));

using var cts = new CancellationTokenSource();
var result = await exporter.ExportAsync(
    schemaPath, outputPath, exportOptions, progressReporter, cts.Token);

// Import with plan preview:
var dataReader = provider.GetRequiredService<ICmtDataReader>();
var data = await dataReader.ReadAsync(dataPath);
var graphBuilder = provider.GetRequiredService<IDependencyGraphBuilder>();
var graph = graphBuilder.Build(data.Schema);
var planBuilder = provider.GetRequiredService<IExecutionPlanBuilder>();
var plan = planBuilder.Build(graph, data.Schema);

var previewDialog = new ExecutionPlanPreviewDialog(plan);
Application.Run(previewDialog);
if (previewDialog.IsApproved)
{
    var importer = provider.GetRequiredService<IImporter>();
    var result = await importer.ImportAsync(data, plan, importOptions, progressReporter, cts.Token);
}
```

---

## Error Handling

### Error Types

| Error | Condition | Recovery |
|-------|-----------|----------|
| File not found | Schema/data file doesn't exist | Inline validation before start |
| Schema mismatch | Missing columns in target | Option to enable SkipMissingColumns |
| Import failure | Record-level error | Error list in results area; ContinueOnError controls behavior |
| Bulk API not supported | Entity doesn't support CreateMultiple | Automatic fallback to individual ops |
| Cancelled | User cancelled operation | Partial results shown; can restart |
| Pool exhausted | Too many parallel entities | Reduce MaxParallelEntities and retry |
| Auth expired | Token expires during long operation | Re-auth dialog; must restart operation |

### Recovery Strategies

- **File validation**: Validate paths before enabling Start button
- **Schema mismatch**: Show warning with option to toggle SkipMissingColumns
- **Record errors**: Collected and displayed in results area; operation continues if ContinueOnError=true
- **Warnings**: Collected via IWarningCollector; shown in results with warning icon
- **Cancellation**: Partial results displayed; clear button resets to configuration state

### Edge Cases

| Scenario | Expected Behavior |
|----------|-------------------|
| Empty data file | Import succeeds with 0 counts |
| All records fail | Results show 0 success, N errors per entity |
| Circular reference in schema | Execution plan shows deferred fields; preview dialog highlights them |
| Very long import (hours) | Progress remains responsive; elapsed timer updates continuously |
| Cancel during Phase 2 | Current batch completes; partial deferred updates shown |
| No entities in schema | Export shows "No entities to export" warning |

---

## Design Decisions

### Why Separate Export and Import Modes?

**Context:** Export and import are distinct operations with different configuration, progress, and results. Could be two separate screens or one screen with mode selection.

**Decision:** Single screen with RadioGroup mode selection. Export and import share the progress and results areas; only the configuration panel changes.

**Alternatives considered:**
- Two separate screens: Rejected — too similar in structure; shared progress/results UI reduces code
- Wizard-style flow: Rejected — adds step-navigation complexity for what is essentially "configure then run"

**Consequences:**
- Positive: Single menu entry for all migration operations
- Positive: Shared progress and results infrastructure
- Negative: Mode switch resets configuration (acceptable — different files/options per mode)

### Why Execution Plan Preview Dialog?

**Context:** Import ordering is critical for correctness. Users should understand what will happen before starting a potentially hours-long operation.

**Decision:** Modal dialog showing the execution plan: tiers with entities, deferred fields, M2M relationships. User must approve (Proceed) or cancel.

**Alternatives considered:**
- Inline plan display: Possible but takes screen space from progress area
- Skip preview: Rejected — users need confidence in the plan before committing
- Auto-start with plan in log: Rejected — no opportunity to cancel before execution

**Consequences:**
- Positive: Full transparency of import strategy
- Positive: Users can identify potential issues (e.g., unexpected deferred fields)
- Negative: Extra dialog step before import starts (acceptable for safety)

### Why Custom TuiMigrationProgressReporter?

**Context:** `IProgressReporter` fires callbacks on background threads. TUI updates must happen on the main thread. The existing `TuiOperationProgress` handles `IOperationProgress` but migration uses the richer `IProgressReporter`.

**Decision:** Custom `TuiMigrationProgressReporter` that implements `IProgressReporter` and marshals all callbacks via `Application.MainLoop.Invoke()`.

**Alternatives considered:**
- Wrap IProgressReporter in IOperationProgress adapter: Rejected — loses phase/rate/ETA information
- Post-hoc polling instead of callbacks: Rejected — stale data, polling overhead

**Consequences:**
- Positive: Full migration metrics (phase, rate, ETA) displayed in TUI
- Positive: Thread-safe by construction (all updates marshaled)
- Negative: Another progress implementation to maintain

### Why Not Resumable Operations?

**Context:** Large imports can take hours. If cancelled, the user must start over.

**Decision:** No resume support in v1. Migration operations are atomic per-entity; partial results are consistent but cannot be continued.

**Consequences:**
- Positive: Simpler implementation
- Negative: Cancelled imports must restart (mitigated by upsert mode — already-imported records are skipped)
- Future: Resume support is on the roadmap

---

## Configuration

Import options exposed in the configuration panel:

| Setting | UI Control | Default | Maps to ImportOptions |
|---------|-----------|---------|----------------------|
| Mode | RadioGroup | Upsert | `Mode` |
| Use Bulk APIs | Checkbox | true | `UseBulkApis` |
| Max Parallel Entities | Spinner (1-16) | 4 | `MaxParallelEntities` |
| Bypass Custom Plugins | ComboBox | None | `BypassCustomPlugins` |
| Bypass Power Automate Flows | Checkbox | false | `BypassPowerAutomateFlows` |
| Continue on Error | Checkbox | true | `ContinueOnError` |
| Skip Missing Columns | Checkbox | false | `SkipMissingColumns` |
| Strip Owner Fields | Checkbox | false | `StripOwnerFields` |
| Suppress Duplicate Detection | Checkbox | false | `SuppressDuplicateDetection` |

Export options:

| Setting | UI Control | Default | Maps to ExportOptions |
|---------|-----------|---------|----------------------|
| Parallelism | Spinner (1-16) | CPU × 2 | `DegreeOfParallelism` |
| Page Size | Spinner (100-10000) | 5000 | `PageSize` |

---

## Testing

### Acceptance Criteria

- [ ] Screen loads in Export mode by default
- [ ] Mode switch (Export/Import) changes configuration panel
- [ ] Schema file path validated before enabling Start
- [ ] Data file path validated before enabling Start
- [ ] Ctrl+P opens ExecutionPlanPreviewDialog for import mode
- [ ] Execution plan dialog shows tiers, deferred fields, M2M counts
- [ ] Ctrl+Enter starts operation on background thread
- [ ] Progress area shows phase, entity, record count, rate, ETA
- [ ] Results area shows per-entity success/failure counts after completion
- [ ] Cancel button stops operation with partial results
- [ ] TuiMigrationProgressReporter marshals all updates to UI thread
- [ ] State capture returns accurate MigrationScreenState

### Edge Cases

| Scenario | Input | Expected Output |
|----------|-------|-----------------|
| No schema file selected | Empty path field | Start button disabled |
| Schema with 1 entity | Single entity export | Progress shows 1/1 entity |
| Import with no deferred fields | Simple schema | Plan dialog shows 0 deferred |
| Cancel immediately | Cancel after start | Operation stops; partial results |
| All entities fail import | Bad data for all records | Results show 0 success per entity |
| Very large file (500MB) | Large CMT archive | File read progress before import starts |

### Test Examples

```csharp
[Fact]
[Trait("Category", "TuiUnit")]
public void MigrationScreen_DefaultsToExportMode()
{
    var session = CreateMockSession();
    var screen = new MigrationScreen(session, new TuiErrorService());

    var state = screen.CaptureState();

    Assert.Equal(MigrationMode.Export, state.Mode);
    Assert.Equal(MigrationOperationState.Idle, state.OperationState);
}

[Fact]
[Trait("Category", "TuiUnit")]
public void MigrationScreen_ValidatesSchemaPath()
{
    var session = CreateMockSession();
    var screen = new MigrationScreen(session, new TuiErrorService());
    screen.SetSchemaPath("nonexistent.xml");

    var state = screen.CaptureState();

    Assert.NotNull(state.ErrorMessage);  // Validation error
}

[Fact]
[Trait("Category", "TuiUnit")]
public void ExecutionPlanPreviewDialog_CapturesTierCounts()
{
    var plan = new ExecutionPlan
    {
        Tiers = new[]
        {
            new ImportTier { Entities = new[] { "businessunit", "team" } },
            new ImportTier { Entities = new[] { "account" } },
            new ImportTier { Entities = new[] { "contact" } }
        },
        DeferredFields = new Dictionary<string, IReadOnlyList<string>>
        {
            ["account"] = new[] { "parentaccountid" }
        },
        ManyToManyRelationships = new[] { new RelationshipSchema { Name = "systemuserroles" } }
    };
    var dialog = new ExecutionPlanPreviewDialog(plan);

    var state = dialog.CaptureState();

    Assert.Equal(3, state.TierCount);
    Assert.Equal(4, state.EntityCount);
    Assert.Equal(1, state.DeferredFieldCount);
    Assert.Equal(1, state.M2MRelationshipCount);
}

[Fact]
[Trait("Category", "TuiUnit")]
public void TuiMigrationProgressReporter_InvokesOnProgress()
{
    ProgressEventArgs? received = null;
    var reporter = new TuiMigrationProgressReporter(
        onProgress: args => received = args,
        onComplete: _ => { },
        onError: (_, _) => { });

    reporter.Report(new ProgressEventArgs { Phase = "Entity Import" });

    // In real test, Application.MainLoop.Invoke would be mocked
    Assert.NotNull(received);
}
```

---

## Related Specs

- [migration.md](./migration.md) - Backend: IExporter, IImporter, IDependencyGraphBuilder, IExecutionPlanBuilder, CMT format
- [bulk-operations.md](./bulk-operations.md) - Infrastructure consumed by importer
- [tui.md](./tui.md) - TUI framework: ITuiScreen, TuiDialog, TuiSpinner, state capture
- [architecture.md](./architecture.md) - IProgressReporter pattern, Application Service boundary
- [connection-pooling.md](./connection-pooling.md) - Pool parallelism for concurrent import

---

## Roadmap

- Resume support for interrupted imports with checkpoint files
- Drag-and-drop file selection (Terminal.Gui file drag support)
- Migration profile presets (save/load configuration sets)
- Diff view comparing source data against target environment
- Scheduled migration runs with cron-like configuration
