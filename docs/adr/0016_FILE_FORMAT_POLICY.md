# ADR-0016: File Format Policy

**Status:** Accepted
**Date:** 2026-01-07
**Authors:** Josh, Claude

## Context

The CLI produces and consumes various file formats. Without a clear policy, we risk:
- Format inconsistency (some JSON, some YAML, some XML)
- User confusion about what format to use
- Dependency bloat from supporting multiple formats

Related decisions:
- ADR-0008: CLI output architecture (stdout=JSON for data)
- ADR-0020: Import error reporting (JSON error reports)

## Decision

### Format by Use Case

| Use Case | Format | Extension | Example |
|----------|--------|-----------|---------|
| CLI stdout output | JSON | (piped) | `ppds data export --output-format json` |
| User config files | JSON | `.json` | `registrations.json`, `mapping.json` |
| Streaming structured data | JSON Lines | `.jsonl` | `import.errors.jsonl` |
| Human-readable logs | Plain text | `.log` | `import.progress.log` |
| Summary reports | JSON | `.json` | `import.summary.json` |
| Tabular data export | CSV | `.csv` | `ppds data export --output-format csv` |
| Query input files | Native | `.fetchxml`, `.sql` | Domain-specific formats |

### JSON for Config Files

All user-authored configuration files use JSON:
- `registrations.json` - Plugin registration
- `mapping.json` - CSV column mappings
- `deployment-settings.json` - PAC-compatible settings
- Filter files - Query filters

**YAML is explicitly not supported.** Rationale:
- Consistency with CLI output format
- No additional dependencies (YamlDotNet)
- JSON Schema provides IDE autocomplete/validation
- Single format to document and support

Mitigations for lack of comments:
1. JSON Schema files (`schemas/*.schema.json`) for IDE support
2. `_`-prefixed metadata fields in generated files
3. Example snippets in CLI help

### JSON Lines for Streaming

Use JSONL (one JSON object per line) for streaming structured data:
- Append-only writes (safe during crashes)
- Line-by-line parsing (memory efficient)
- Easy to grep/tail

Example: `import.errors.jsonl`
```
{"recordId":"abc","entity":"account","error":"Duplicate key"}
{"recordId":"def","entity":"contact","error":"Missing reference"}
```

## Consequences

### Positive
- Clear guidance for all file format decisions
- Consistency across the CLI
- No YAML dependency
- JSON Schema ecosystem for config validation

### Negative
- Users cannot add comments to config files
- Must use JSON syntax (quotes, commas)

### Neutral
- Can revisit YAML support if users request it
