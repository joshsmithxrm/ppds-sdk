# ADR-0012: Hybrid Filter Design for Plugin Traces

**Status:** Accepted
**Date:** 2026-01-04
**Authors:** Josh, Claude

## Context

The `ppds plugintraces list` command needs to support filtering across 25 fields with 11 operators. The extension's Plugin Trace Viewer demonstrates two distinct usage patterns:

1. **Quick filtering** - "Show me exceptions from the last hour"
2. **Complex queries** - "Find slow plugins with specific conditions across multiple fields"

Supporting all 25 fields × 11 operators as CLI flags would be unwieldy:

```bash
# What we want to avoid
ppds plugintraces list --typename-contains "My" --duration-gt 1000 --mode-eq 0
```

Conversely, requiring a filter file for every query creates friction for simple cases.

### Filter Fields (25)

| Category | Fields |
|----------|--------|
| Core | plugintracelogid, createdon, typename, primaryentity, messagename, operationtype, mode, stage |
| Performance | performanceexecutionduration, performanceconstructorduration, performanceexecutionstarttime, performanceconstructorstarttime |
| Execution | exceptiondetails, messageblock, configuration, secureconfiguration, profile |
| Correlation | correlationid, requestid, pluginstepid, persistencekey, organizationid |
| Audit | issystemcreated, _createdby_value, _createdonbehalfby_value, depth |

### Filter Operators (11)

| Type | Operators |
|------|-----------|
| Comparison | eq, ne, gt, lt, ge, le |
| Text | contains, startswith, endswith |
| Null | null, notnull |

## Decision

Implement a **hybrid filter approach**:

1. **Inline flags** for the 10 most common filters (80/20 rule)
2. **Quick filter shortcuts** for 8 common scenarios
3. **Filter file** for complex multi-condition queries

### Inline Filters (10)

| Flag | Field | Operator | Rationale |
|------|-------|----------|-----------|
| `--plugin` | typename | contains | Most common - find traces from specific plugin |
| `--entity` | primaryentity | eq | Second most common - filter by entity |
| `--message` | messagename | eq | Filter by message (Create, Update, etc) |
| `--mode` | mode | eq | Quick sync/async filtering |
| `--since` | createdon | ge | Time-based filtering is universal |
| `--until` | createdon | le | Time range queries |
| `--min-duration` | performanceexecutionduration | gt | Performance debugging |
| `--correlation` | correlationid | eq | Troubleshooting specific executions |
| `--depth` | depth | eq | Recursive execution debugging |
| `--has-exception` | exceptiondetails | notnull | Quick error filtering |

### Quick Filter Shortcuts (8)

| Flag | Condition | Use Case |
|------|-----------|----------|
| `--exceptions` | exceptiondetails is not null | Find failures |
| `--success` | exceptiondetails is null | Find successes |
| `--last-hour` | createdon >= (now - 1h) | Recent traces |
| `--last-24h` | createdon >= (now - 24h) | Today's traces |
| `--today` | createdon >= midnight | Current day |
| `--async-only` | mode = 1 | Async plugins only |
| `--sync-only` | mode = 0 | Sync plugins only |
| `--recursive` | depth > 1 | Nested plugin calls |

### Filter File Schema

```json
{
  "conditions": [
    { "field": "typename", "operator": "contains", "value": "MyPlugin" },
    { "field": "performanceexecutionduration", "operator": "gt", "value": 1000 }
  ],
  "logicalOperator": "and",
  "orderBy": { "field": "createdon", "descending": true },
  "top": 100
}
```

### Combining Behavior

All filter sources combine with AND logic:

```bash
ppds plugintraces list --plugin MyPlugin --exceptions --filter-file extra.json
# => typename contains 'MyPlugin' AND exceptiondetails IS NOT NULL AND <file conditions>
```

**Priority when same field appears in multiple sources:**
1. Filter file conditions take precedence
2. Inline flags applied second
3. Quick filters applied last

## Rationale

### Why inline flags for common filters?

- Immediate discoverability via `--help`
- Tab completion support in shells
- Muscle memory for frequent operations
- Consistent with existing CLI patterns (`--filter` in metadata commands)

### Why quick filter shortcuts?

- Named presets are self-documenting
- Avoids datetime parsing for time-based filters
- Maps directly to extension's quick filter buttons
- Single flag replaces complex condition

### Why filter file for complex queries?

- Unlimited condition combinations
- OR logic support (not possible with AND-only inline flags)
- Saveable and shareable queries
- Editor support with JSON schema validation
- Version controllable query definitions

## Consequences

### Positive

- Simple queries stay simple: `ppds plugintraces list --exceptions --last-hour`
- Complex queries are possible with full operator support
- Consistent with extension's UX model (quick filters + advanced)
- Filter files can be version-controlled and shared

### Negative

- Two learning curves (flags + file format)
- Filter file requires documentation and schema
- Users may not discover filter file capability

## Alternatives Considered

### All flags for all fields

```bash
ppds plugintraces list --typename-contains "My" --duration-gt 1000 --mode-eq 0
```

Rejected because:
- 25+ flags with operator suffixes would be unusable
- Tab completion becomes overwhelming
- Help output would be unreadable

### Filter file only

Rejected because:
- Simple queries like "show exceptions" would require creating a file
- Friction for exploratory debugging
- Poor developer experience for common cases

### Query string syntax

```bash
ppds plugintraces list --filter "typename contains 'My' and duration gt 1000"
```

Rejected because:
- Requires careful quoting and escaping in shell
- No tab completion for field names
- Error-prone for complex queries
- Harder to validate than structured JSON

### OData $filter parameter

```bash
ppds plugintraces list --odata-filter "contains(typename,'My') and duration gt 1000"
```

Rejected because:
- OData syntax is unfamiliar to many developers
- Function syntax varies between operators
- Shell escaping issues with parentheses

## References

- [ADR-0008: CLI Output Architecture](0008_CLI_OUTPUT_ARCHITECTURE.md) - Output stream conventions
- [ADR-0009: CLI Command Taxonomy](0009_CLI_COMMAND_TAXONOMY.md) - Why `plugintraces` naming
- Extension source: `extension/src/features/pluginTraceViewer/`
