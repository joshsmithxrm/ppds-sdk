# Design: Plugin Traces (Phase 3)

## Issues

| # | Title | Type | Priority | Size |
|---|-------|------|----------|------|
| 152 | feat: Add `ppds plugintraces list` command with inline filters | feature | P2-Medium | L |
| 153 | feat: Add `ppds plugintraces get` command | feature | P2-Medium | M |
| 154 | feat: Add `ppds plugintraces related` command | feature | P3-Low | M |
| 155 | feat: Add filter file support for `ppds plugintraces list` | feature | P3-Low | S |
| 156 | feat: Add `ppds plugintraces delete` command | feature | P3-Low | S |
| 157 | feat: Add `ppds plugintraces settings` command | feature | P2-Medium | S |
| 158 | feat: Add CSV export support for `ppds plugintraces list` | feature | P3-Low | S |

## Context

Plugin traces are critical for debugging Dataverse plugins. The `PluginTraceLog` entity stores execution details. This phase adds CLI commands to query, view, and manage plugin traces.

### Dataverse Entity
- **Entity**: `plugintracelog`
- **Key Fields**: `performanceexecutionstarttime`, `messagename`, `primaryentity`, `exceptiondetails`, `messageblock`
- **Generated Class**: Check `src/PPDS.Dataverse/Generated/` for `PluginTraceLog`

## Command Structure

```
ppds plugintraces
├── list       # List traces with filters
├── get        # Get single trace details
├── related    # Find traces related to a correlation ID or record
├── delete     # Delete old traces
└── settings   # View/modify trace settings
```

## Implementation Order

1. **#152 - list** (foundation - others depend on this)
2. **#153 - get** (simple follow-on)
3. **#157 - settings** (independent, system-level)
4. **#154 - related** (builds on list)
5. **#155 - filter file** (enhancement to list)
6. **#156 - delete** (administrative)
7. **#158 - CSV export** (enhancement to list)

## Key Files to Create/Modify

```
src/PPDS.Dataverse/Services/IPluginTraceService.cs (new)
src/PPDS.Dataverse/Services/PluginTraceService.cs (new)
src/PPDS.Cli/Commands/PluginTracesCommand.cs (new)
src/PPDS.Cli/Commands/PluginTraces/ListCommand.cs (new)
src/PPDS.Cli/Commands/PluginTraces/GetCommand.cs (new)
src/PPDS.Cli/Commands/PluginTraces/RelatedCommand.cs (new)
src/PPDS.Cli/Commands/PluginTraces/DeleteCommand.cs (new)
src/PPDS.Cli/Commands/PluginTraces/SettingsCommand.cs (new)
```

## Technical Notes

### #152 - list command
```bash
ppds plugintraces list [options]

Options:
  --entity <name>       Filter by primary entity
  --message <name>      Filter by message (Create, Update, etc.)
  --plugin <name>       Filter by plugin type name
  --since <datetime>    Traces after this time
  --until <datetime>    Traces before this time
  --errors-only         Only show traces with exceptions
  --limit <n>           Max results (default 50)
  --output-format       json|table|csv
```

### #153 - get command
```bash
ppds plugintraces get <trace-id>

# Shows full details including:
# - Message block (inputs/outputs)
# - Exception details with stack trace
# - Performance metrics
# - Correlation ID
```

### #154 - related command
```bash
ppds plugintraces related --correlation-id <id>
ppds plugintraces related --record <entity>/<guid>

# Finds traces that share correlation ID
# or operated on the same record
```

### #155 - filter file support
```yaml
# traces-filter.yaml
entity: account
messages:
  - Create
  - Update
since: 2024-01-01
errors_only: true
```

```bash
ppds plugintraces list --filter traces-filter.yaml
```

### #156 - delete command
```bash
ppds plugintraces delete --older-than 30d
ppds plugintraces delete --all --confirm

# Requires confirmation for destructive operations
```

### #157 - settings command
```bash
ppds plugintraces settings
# Shows: Trace level, retention, enabled plugins

ppds plugintraces settings --enable <plugin-type>
ppds plugintraces settings --level Exception|All|Off
```

### #158 - CSV export
```bash
ppds plugintraces list --output-format csv > traces.csv
```

## Service Layer Pattern

Follow existing patterns in `src/PPDS.Dataverse/Services/`:

```csharp
public interface IPluginTraceService
{
    Task<IReadOnlyList<PluginTraceLog>> ListTracesAsync(
        PluginTraceFilter filter,
        CancellationToken cancellationToken = default);

    Task<PluginTraceLog?> GetTraceAsync(
        Guid traceId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PluginTraceLog>> GetRelatedTracesAsync(
        Guid correlationId,
        CancellationToken cancellationToken = default);

    Task<int> DeleteTracesAsync(
        PluginTraceFilter filter,
        CancellationToken cancellationToken = default);

    Task<PluginTraceSettings> GetSettingsAsync(
        CancellationToken cancellationToken = default);

    Task SetSettingsAsync(
        PluginTraceSettings settings,
        CancellationToken cancellationToken = default);
}
```

## Acceptance Criteria

### #152 - list
- [ ] Lists traces with default limit of 50
- [ ] All filter options work (--entity, --message, --plugin, --since, --until, --errors-only)
- [ ] JSON and table output formats
- [ ] Performance: <2s for typical queries

### #153 - get
- [ ] Shows full trace details
- [ ] Message block formatted for readability
- [ ] Exception stack trace properly formatted
- [ ] Returns error for non-existent trace ID

### #154 - related
- [ ] Finds traces by correlation ID
- [ ] Finds traces by record (entity/guid)
- [ ] Results include original trace if found

### #155 - filter file
- [ ] YAML format supported
- [ ] Validates filter file schema
- [ ] CLI args override file values

### #156 - delete
- [ ] --older-than works with various formats (30d, 1w, 24h)
- [ ] Requires --confirm for destructive operations
- [ ] Reports count of deleted traces

### #157 - settings
- [ ] Displays current trace settings
- [ ] Can modify trace level
- [ ] Can enable/disable per-plugin tracing

### #158 - CSV export
- [ ] CSV output format works
- [ ] Headers included
- [ ] Special characters properly escaped
