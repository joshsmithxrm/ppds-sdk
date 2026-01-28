# Plugin Traces

**Status:** Implemented
**Version:** 1.0
**Last Updated:** 2026-01-28
**Code:** [src/PPDS.Dataverse/Services/](../src/PPDS.Dataverse/Services/) | [src/PPDS.Cli/Commands/PluginTraces/](../src/PPDS.Cli/Commands/PluginTraces/)

---

## Overview

The plugin traces system provides querying, inspection, and management of Dataverse plugin trace logs. It supports filtered listing, detailed trace inspection, execution timeline visualization with depth-based hierarchy, trace log settings management, and bulk deletion with progress reporting.

### Goals

- **Diagnostics**: Query and filter plugin trace logs for debugging plugin execution issues
- **Timeline**: Visualize plugin execution chains as hierarchical timelines using correlation IDs
- **Management**: Delete traces (single, filtered, bulk) and control trace logging settings
- **Performance**: Identify slow plugins via duration filtering and execution metrics

### Non-Goals

- Plugin registration management (handled by [plugins.md](./plugins.md))
- Real-time trace streaming (traces are queried after execution)
- Plugin profiling or replay (handled by Plugin Registration Tool)

---

## Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                     Application Layer                            │
│            (CLI: ppds plugintraces list/get/delete/...)          │
└──────────────────────────────┬──────────────────────────────────┘
                               │
         ┌─────────────────────┴─────────────────────┐
         │                                           │
         ▼                                           ▼
┌──────────────────────────┐          ┌──────────────────────────────┐
│   IPluginTraceService    │          │  TimelineHierarchyBuilder    │
│   (query, delete,        │          │  (depth-based hierarchy,     │
│    settings, count)      │          │   positioning calculation)   │
└────────────┬─────────────┘          └──────────────────────────────┘
             │
             ▼
┌──────────────────────────┐
│ IDataverseConnectionPool │
│ (FetchXml + QueryExpr)   │
└────────────┬─────────────┘
             │
             ▼
┌──────────────────────────┐
│     Dataverse            │
│   (plugintracelog entity) │
└──────────────────────────┘
```

### Components

| Component | Responsibility |
|-----------|----------------|
| `PluginTraceService` | Query, delete, settings operations via connection pool |
| `TimelineHierarchyBuilder` | Static utility: builds depth-based timeline hierarchies |
| CLI Commands (6) | list, get, related, timeline, settings, delete |

### Dependencies

- Depends on: [connection-pooling.md](./connection-pooling.md) for pooled Dataverse clients
- Depends on: [authentication.md](./authentication.md) for environment connection
- Uses patterns from: [architecture.md](./architecture.md) for Application Service layer

---

## Specification

### Core Requirements

1. **Filtered listing**: Query traces with 16 filter criteria including type, message, entity, mode, time range, duration range, error state, and correlation
2. **Detail inspection**: Retrieve full trace details including exception stack traces, trace output, and configuration
3. **Related traces**: Find all traces sharing a correlation ID for request-level debugging
4. **Timeline**: Build hierarchical execution tree from flat traces using Dataverse execution depth
5. **Settings**: Read and update the organization-level plugin trace log setting (Off/Exception/All)
6. **Deletion**: Delete single, by IDs, by filter, by age, or all traces with progress reporting
7. **Count**: Count matching traces for dry-run deletion previews

### Primary Flows

**Trace Investigation:**

1. **List traces**: `ppds plugintraces list --errors-only --last-hour` to find recent failures
2. **Get details**: `ppds plugintraces get <trace-id>` to see exception and trace output
3. **View timeline**: `ppds plugintraces timeline <trace-id>` to see full execution chain
4. **Find related**: `ppds plugintraces related <trace-id>` to see all traces from same request

**Trace Cleanup:**

1. **Preview**: `ppds plugintraces delete --older-than 7d --dry-run` to count traces
2. **Delete**: `ppds plugintraces delete --older-than 7d` to remove old traces
3. **Delete all**: `ppds plugintraces delete --all --force` to clear all traces

### Constraints

- Plugin trace logging must be enabled in the environment (settings set to Exception or All)
- Traces are created by the platform, not by this system
- `plugintracelog` entity has OData limitations; FetchXml is used for count operations
- Bulk deletion uses parallel requests via connection pool

### Validation Rules

| Field | Rule | Error |
|-------|------|-------|
| `trace-id` | Must be valid GUID | `ArgumentException` |
| `--older-than` | Must be valid duration (e.g., 7d, 24h, 30m) | Parse error |
| `--all` | Requires `--force` flag | Error message |
| `--record` | Format: `entity` or `entity/guid` | Parse error with example |

---

## Core Types

### IPluginTraceService

Service for querying and managing plugin trace logs ([`IPluginTraceService.cs:11-139`](../src/PPDS.Dataverse/Services/IPluginTraceService.cs#L11-L139)).

```csharp
public interface IPluginTraceService
{
    // Query Operations
    Task<List<PluginTraceInfo>> ListAsync(
        PluginTraceFilter? filter = null, int top = 100,
        CancellationToken cancellationToken = default);
    Task<PluginTraceDetail?> GetAsync(
        Guid traceId, CancellationToken cancellationToken = default);
    Task<List<PluginTraceInfo>> GetRelatedAsync(
        Guid correlationId, int top = 1000,
        CancellationToken cancellationToken = default);
    Task<List<TimelineNode>> BuildTimelineAsync(
        Guid correlationId, CancellationToken cancellationToken = default);

    // Delete Operations
    Task<bool> DeleteAsync(
        Guid traceId, CancellationToken cancellationToken = default);
    Task<int> DeleteByIdsAsync(
        IEnumerable<Guid> traceIds, IProgress<int>? progress = null,
        CancellationToken cancellationToken = default);
    Task<int> DeleteByFilterAsync(
        PluginTraceFilter filter, IProgress<int>? progress = null,
        CancellationToken cancellationToken = default);
    Task<int> DeleteAllAsync(
        IProgress<int>? progress = null,
        CancellationToken cancellationToken = default);
    Task<int> DeleteOlderThanAsync(
        TimeSpan olderThan, IProgress<int>? progress = null,
        CancellationToken cancellationToken = default);

    // Settings
    Task<PluginTraceSettings> GetSettingsAsync(
        CancellationToken cancellationToken = default);
    Task SetSettingsAsync(
        PluginTraceLogSetting setting,
        CancellationToken cancellationToken = default);

    // Count
    Task<int> CountAsync(
        PluginTraceFilter? filter = null,
        CancellationToken cancellationToken = default);
}
```

The implementation ([`PluginTraceService.cs`](../src/PPDS.Dataverse/Services/PluginTraceService.cs)) uses `IDataverseConnectionPool` for all Dataverse access, with QueryExpression for list/filter operations and FetchXml for count operations.

### PluginTraceInfo

Summary record for list views ([`IPluginTraceService.cs:144-184`](../src/PPDS.Dataverse/Services/IPluginTraceService.cs#L144-L184)).

```csharp
public record PluginTraceInfo
{
    public required Guid Id { get; init; }
    public required string TypeName { get; init; }
    public string? MessageName { get; init; }
    public string? PrimaryEntity { get; init; }
    public PluginTraceMode Mode { get; init; }
    public PluginTraceOperationType OperationType { get; init; }
    public int Depth { get; init; }
    public required DateTime CreatedOn { get; init; }
    public int? DurationMs { get; init; }
    public bool HasException { get; init; }
    public Guid? CorrelationId { get; init; }
    public Guid? RequestId { get; init; }
    public Guid? PluginStepId { get; init; }
}
```

### PluginTraceDetail

Full trace details, extends PluginTraceInfo ([`IPluginTraceService.cs:189-229`](../src/PPDS.Dataverse/Services/IPluginTraceService.cs#L189-L229)).

```csharp
public sealed record PluginTraceDetail : PluginTraceInfo
{
    public int? ConstructorDurationMs { get; init; }
    public DateTime? ExecutionStartTime { get; init; }
    public DateTime? ConstructorStartTime { get; init; }
    public string? ExceptionDetails { get; init; }
    public string? MessageBlock { get; init; }
    public string? Configuration { get; init; }
    public string? SecureConfiguration { get; init; }
    public string? Profile { get; init; }
    public Guid? OrganizationId { get; init; }
    public Guid? PersistenceKey { get; init; }
    public bool IsSystemCreated { get; init; }
    public Guid? CreatedById { get; init; }
    public Guid? CreatedOnBehalfById { get; init; }
}
```

### PluginTraceFilter

Filter criteria for trace queries ([`IPluginTraceService.cs:234-283`](../src/PPDS.Dataverse/Services/IPluginTraceService.cs#L234-L283)).

```csharp
public sealed record PluginTraceFilter
{
    public string? TypeName { get; init; }            // Contains match
    public string? MessageName { get; init; }
    public string? PrimaryEntity { get; init; }       // Contains match
    public PluginTraceMode? Mode { get; init; }
    public PluginTraceOperationType? OperationType { get; init; }
    public int? MinDepth { get; init; }
    public int? MaxDepth { get; init; }
    public DateTime? CreatedAfter { get; init; }
    public DateTime? CreatedBefore { get; init; }
    public int? MinDurationMs { get; init; }
    public int? MaxDurationMs { get; init; }
    public bool? HasException { get; init; }
    public Guid? CorrelationId { get; init; }
    public Guid? RequestId { get; init; }
    public Guid? PluginStepId { get; init; }
    public string? OrderBy { get; init; }             // Default: "createdon desc"
}
```

### PluginTraceSettings

Current trace logging configuration ([`IPluginTraceService.cs:330-343`](../src/PPDS.Dataverse/Services/IPluginTraceService.cs#L330-L343)).

```csharp
public sealed record PluginTraceSettings
{
    public required PluginTraceLogSetting Setting { get; init; }
    public string SettingName => Setting switch { ... }; // Computed: "Off", "Exception", "All"
}
```

### TimelineNode

Node in the plugin execution timeline hierarchy ([`IPluginTraceService.cs:348-364`](../src/PPDS.Dataverse/Services/IPluginTraceService.cs#L348-L364)).

```csharp
public sealed record TimelineNode
{
    public required PluginTraceInfo Trace { get; init; }
    public IReadOnlyList<TimelineNode> Children { get; init; } = Array.Empty<TimelineNode>();
    public int HierarchyDepth { get; init; }       // 0-based (converted from Dataverse 1-based depth)
    public double OffsetPercent { get; init; }      // Timeline visualization offset
    public double WidthPercent { get; init; }       // Timeline visualization width
}
```

### Enums

```csharp
public enum PluginTraceMode
{
    Synchronous = 0,    // Blocks user transaction
    Asynchronous = 1    // Background processing
}

public enum PluginTraceOperationType
{
    Unknown = 0,
    Plugin = 1,
    WorkflowActivity = 2
}

public enum PluginTraceLogSetting
{
    Off = 0,            // No tracing
    Exception = 1,      // Log only exceptions
    All = 2             // Log all executions
}
```

### TimelineHierarchyBuilder

Static utility for building hierarchical timelines from flat trace records ([`TimelineHierarchyBuilder.cs`](../src/PPDS.Dataverse/Services/TimelineHierarchyBuilder.cs)).

```csharp
public static class TimelineHierarchyBuilder
{
    // Build hierarchy from flat traces using execution depth
    static List<TimelineNode> Build(IReadOnlyList<PluginTraceInfo> traces);

    // Build hierarchy with offset/width positioning pre-calculated
    static List<TimelineNode> BuildWithPositioning(IReadOnlyList<PluginTraceInfo> traces);

    // Get total duration span across all traces (ms)
    static long GetTotalDuration(IReadOnlyList<PluginTraceInfo> traces);

    // Count total nodes including descendants
    static int CountTotalNodes(IReadOnlyList<TimelineNode> roots);
}
```

### Usage Pattern

```csharp
var traceService = serviceProvider.GetRequiredService<IPluginTraceService>();

// List recent errors
var errors = await traceService.ListAsync(
    new PluginTraceFilter { HasException = true, CreatedAfter = DateTime.UtcNow.AddHours(-1) });

// Get full details
var detail = await traceService.GetAsync(errors[0].Id);

// Build timeline from correlation
if (detail?.CorrelationId is { } corrId)
{
    var timeline = await traceService.BuildTimelineAsync(corrId);
    // timeline is a tree: root nodes with Children
}

// Cleanup old traces
var deleted = await traceService.DeleteOlderThanAsync(
    TimeSpan.FromDays(30), new Progress<int>(n => Console.Write($"\rDeleted {n}")));
```

---

## CLI Commands

All commands accept `--profile` and `--environment` options for authentication.

### `ppds plugintraces list`

Lists traces with comprehensive filtering. Supports CSV and JSON output formats.

| Option | Description |
|--------|-------------|
| `--type, -t` | Filter by plugin type name (contains) |
| `--message, -m` | Filter by message name (Create, Update, etc.) |
| `--entity` | Filter by primary entity (contains) |
| `--mode` | Filter by execution mode: sync or async |
| `--errors-only` | Show only traces with exceptions |
| `--success-only` | Show only successful traces |
| `--since` | Traces created after (ISO 8601) |
| `--until` | Traces created before (ISO 8601) |
| `--min-duration` | Minimum execution duration (ms) |
| `--max-duration` | Maximum execution duration (ms) |
| `--correlation-id` | Filter by correlation ID |
| `--request-id` | Filter by request ID |
| `--step-id` | Filter by plugin step ID |
| `--last-hour` | Shortcut: traces from last hour |
| `--last-24h` | Shortcut: traces from last 24 hours |
| `--async-only` | Show only asynchronous traces |
| `--recursive` | Show only nested traces (depth > 1) |
| `--record` | Filter by record (entity or entity/guid) |
| `--filter` | JSON file with filter criteria |
| `--top, -n` | Max results (default: 100) |
| `--order-by` | Sort field (default: createdon desc) |

### `ppds plugintraces get <trace-id>`

Displays full trace details: basic info, timing, correlation, exception details, trace output, and configuration.

### `ppds plugintraces related`

Finds all traces sharing a correlation ID.

| Option | Description |
|--------|-------------|
| `<trace-id>` | Get correlation ID from this trace (optional) |
| `--correlation-id` | Direct correlation ID filter |
| `--record` | Filter by record (entity or entity/guid) |

### `ppds plugintraces timeline`

Displays plugin execution as a hierarchical tree showing parent-child relationships and timing.

| Option | Description |
|--------|-------------|
| `<trace-id>` | Get correlation ID from this trace (optional) |
| `--correlation-id` | Direct correlation ID filter |

### `ppds plugintraces settings get`

Shows the current plugin trace logging setting (Off, Exception, or All).

### `ppds plugintraces settings set <value>`

Updates the organization-level trace logging setting. Values: `off`, `exception`, `all`.

### `ppds plugintraces delete`

Deletes plugin trace logs with multiple modes.

| Option | Description |
|--------|-------------|
| `<trace-id>` | Delete a single trace by ID |
| `--ids` | Comma-separated list of trace IDs |
| `--older-than` | Delete traces older than duration (7d, 24h, 30m) |
| `--all` | Delete ALL traces (requires --force) |
| `--dry-run` | Preview count without deleting |
| `--force` | Skip confirmation for --all |

---

## Error Handling

### Error Types

| Error | Condition | Recovery |
|-------|-----------|----------|
| Trace not found | Invalid trace ID | Returns null (get) or false (delete) |
| Settings update failed | Insufficient privileges | Requires System Administrator role |
| Deletion failed | Service protection limits | Automatic retry via connection pool |
| Filter parse error | Invalid --record or --older-than format | Error message with expected format |

### Edge Cases

| Scenario | Expected Behavior |
|----------|-------------------|
| No traces match filter | Return empty list, count returns 0 |
| Delete non-existent trace | Return false (not found) |
| --all without --force | Error: "--all requires --force" |
| Depth = 1 trace | Root node in timeline (HierarchyDepth = 0) |
| No correlation ID | Timeline returns single-node list |

---

## Design Decisions

### Why Depth-Based Timeline Hierarchy?

**Context:** Dataverse traces include an `Execution Depth` field (1 = top-level, 2 = called by depth 1, etc.) and a `Correlation ID` grouping related traces.

**Decision:** Build parent-child hierarchy using stack-based depth tracking on chronologically sorted traces. Convert 1-based Dataverse depth to 0-based hierarchy depth.

**Algorithm:**
1. Sort traces by CreatedOn ascending
2. For each trace, pop stack entries at same or greater depth (siblings)
3. If stack empty, trace is root; otherwise, child of stack top
4. Push trace onto stack as potential parent

**Consequences:**
- Positive: Accurate hierarchy from flat data without explicit parent references
- Positive: Handles arbitrary nesting depth
- Negative: Assumes chronological ordering reflects call order (valid for Dataverse)

### Why IProgress\<int\> Instead of IProgressReporter?

**Context:** Delete operations report a single metric: count of deleted traces. Migration's `IProgressReporter` is designed for multi-entity, multi-phase operations with rich metrics.

**Decision:** Use `IProgress<int>` from the BCL for single-metric progress. Simpler interface, no migration dependency.

**Consequences:**
- Positive: No coupling to migration library
- Positive: Standard BCL pattern, familiar to .NET developers
- Negative: Cannot report errors or phases (not needed for delete count)

### Why FetchXml for Count Operations?

**Context:** OData `$count` has limitations on the `plugintracelog` entity. QueryExpression with `ReturnTotalRecordCount` also has known issues.

**Decision:** Use FetchXml `aggregate="true"` with `count` for reliable counts.

**Consequences:**
- Positive: Reliable counts across all filter combinations
- Negative: FetchXml requires string construction (mitigated by builder methods)

### Why Parallel Deletion via Connection Pool?

**Context:** Plugin trace tables can contain millions of records. Sequential deletion is impractical.

**Decision:** Use `Parallel.ForEachAsync` with connection pool to delete multiple traces concurrently, reporting progress via `IProgress<int>`.

**Consequences:**
- Positive: Deletion throughput scales with pool size
- Positive: Progress feedback for long operations
- Negative: Service protection limits may throttle (handled by pool retry)

---

## Configuration

### PluginTraceFilter Properties

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `TypeName` | string? | null | Plugin type name (contains match) |
| `MessageName` | string? | null | SDK message name |
| `PrimaryEntity` | string? | null | Entity logical name (contains match) |
| `Mode` | enum? | null | Synchronous or Asynchronous |
| `OperationType` | enum? | null | Plugin or WorkflowActivity |
| `MinDepth` | int? | null | Minimum execution depth |
| `MaxDepth` | int? | null | Maximum execution depth |
| `CreatedAfter` | DateTime? | null | Traces after this time |
| `CreatedBefore` | DateTime? | null | Traces before this time |
| `MinDurationMs` | int? | null | Minimum duration (ms) |
| `MaxDurationMs` | int? | null | Maximum duration (ms) |
| `HasException` | bool? | null | Filter by error state |
| `CorrelationId` | Guid? | null | Correlation ID |
| `RequestId` | Guid? | null | Request ID |
| `PluginStepId` | Guid? | null | Plugin step ID |
| `OrderBy` | string? | null | Sort field (default: "createdon desc") |

---

## Testing

### Acceptance Criteria

- [ ] List returns traces matching all filter criteria combinations
- [ ] Get returns full details including exception and message block
- [ ] Related returns all traces sharing correlation ID
- [ ] Timeline builds correct hierarchy from execution depth
- [ ] Settings get/set reads and updates organization setting
- [ ] Delete removes traces and reports progress
- [ ] Count returns accurate count for dry-run operations
- [ ] --all requires --force flag

### Edge Cases

| Scenario | Input | Expected Output |
|----------|-------|-----------------|
| Empty environment | Any list query | Empty list |
| Single trace | Timeline with 1 trace | Single root node, no children |
| Deep nesting | Depth 1→2→3→2→1 | Two root nodes, first with 2-level subtree |
| Missing duration | Trace with DurationMs=null | Display as "--" or 0 |
| Concurrent deletion | Parallel delete + list | Eventually consistent results |

### Test Examples

```csharp
[Fact]
public void TimelineHierarchyBuilder_BuildsCorrectHierarchy()
{
    var traces = new List<PluginTraceInfo>
    {
        CreateTrace(depth: 1, createdOn: t0),
        CreateTrace(depth: 2, createdOn: t1),
        CreateTrace(depth: 3, createdOn: t2),
        CreateTrace(depth: 2, createdOn: t3),
        CreateTrace(depth: 1, createdOn: t4)
    };

    var roots = TimelineHierarchyBuilder.Build(traces);

    Assert.Equal(2, roots.Count);          // Two root nodes
    Assert.Single(roots[0].Children);       // First root has 1 child
    Assert.Single(roots[0].Children[0].Children); // That child has 1 grandchild
    Assert.Empty(roots[1].Children);        // Second root has no children
}

[Fact]
public async Task ListAsync_FiltersErrorsOnly()
{
    var filter = new PluginTraceFilter { HasException = true };

    var traces = await traceService.ListAsync(filter);

    Assert.All(traces, t => Assert.True(t.HasException));
}
```

---

## Related Specs

- [plugins.md](./plugins.md) - Plugin registration (different from trace inspection)
- [connection-pooling.md](./connection-pooling.md) - Pooled clients for parallel operations
- [architecture.md](./architecture.md) - Application Service layer pattern
- [cli.md](./cli.md) - CLI output formatting and global options

---

## Roadmap

- Real-time trace tailing with polling interval
- Trace export to file (CSV/JSON) for offline analysis
- Aggregate statistics (slowest plugins, error rates by entity)
- TUI trace browser with interactive filtering and timeline visualization
