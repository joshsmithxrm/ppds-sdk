# ADR-0011: Deployment Settings File Format

**Status:** Accepted
**Date:** 2026-01-04
**Authors:** Josh, Claude

## Context

The CLI needs to generate deployment settings files for solution deployment. These files configure environment-specific values (environment variables, connection references) that differ between source and target environments.

Two formats exist in the Power Platform ecosystem:

### 1. Power Platform Build Tools Format

Simple JSON with two arrays:

```json
{
  "EnvironmentVariables": [
    { "SchemaName": "prefix_VariableName", "Value": "value" }
  ],
  "ConnectionReferences": [
    {
      "LogicalName": "prefix_connectionref",
      "ConnectionId": "connection-guid",
      "ConnectorId": "/providers/Microsoft.PowerApps/apis/shared_connector"
    }
  ]
}
```

- Used by `pac solution import --settings-file`
- Simple structure, easy to read and maintain
- Already used in ppds-demo project

### 2. ALM Accelerator Format

Complex array with flattened `UserSettings`:

```json
[
  {
    "DeploymentEnvironmentName": "Validation",
    "DeploymentEnvironmentUrl": "https://contoso-val.crm.dynamics.com/",
    "UserSettings": [
      { "Name": "connectionreference.cat_CDS_Current", "Value": "conn-guid" },
      { "Name": "environmentvariable.cat_Endpoint", "Value": "https://api.com" },
      { "Name": "activateflow.activate.FlowName.guid", "Value": "true" }
    ]
  }
]
```

- Designed for multi-environment Azure DevOps pipelines
- Includes flow activation, canvas app sharing, team configuration
- Requires ALM Accelerator pipeline templates

## Decision

Use **Power Platform Build Tools format** (PAC-compatible).

## Rationale

1. **PAC CLI compatibility**: Works directly with `pac solution import --settings-file`

2. **Simplicity**: Two arrays with clear structure, no flattened naming conventions

3. **Existing usage**: Already used in ppds-demo, validated in production

4. **Scope alignment**: PPDS focuses on developer experience, not full pipeline orchestration

5. **Extension parity**: Matches the extension's deployment settings feature

## Sync Behavior

The CLI implements sync operations that match the extension's behavior:

### Deterministic Sorting

Entries sorted by `StringComparison.Ordinal`:
- Environment variables by `SchemaName`
- Connection references by `LogicalName`

This ensures identical file output on repeated runs, enabling git-friendly diffs.

### Value Preservation

**Critical rule:** Never overwrite existing file values with environment values.

When syncing:
1. Read existing file (if exists)
2. Query environment for current entries
3. For each entry:
   - If exists in file: **preserve file value**
   - If new: add with environment value
   - If removed from solution: remove from file
4. Sort and write

This design treats deployment settings as environment-specific configuration, not derived state.

### Sync Statistics

Return counts for visibility:

```json
{ "added": 2, "removed": 1, "preserved": 5 }
```

### Idempotent Operations

Multiple sync runs produce identical files (sorting + preservation = deterministic output).

## CLI Commands

```bash
# Sync existing file with solution (preserves values)
ppds deployment-settings sync --solution MyApp --file config/prod.deploymentsettings.json

# Generate fresh file (uses environment values)
ppds deployment-settings generate --solution MyApp --output config/template.deploymentsettings.json

# Validate file against solution
ppds deployment-settings validate --file config/prod.deploymentsettings.json --solution MyApp
```

## Alternatives Rejected

### ALM Accelerator Format

**Rejected because:**
- Requires adoption of full ALM Accelerator pipeline templates
- Adds complexity (flattened naming, multi-environment arrays)
- Beyond PPDS scope (pipeline orchestration vs developer experience)

**If needed:** Users can convert manually or use ALM Accelerator tooling directly.

### Custom PPDS Format

**Rejected because:**
- No ecosystem compatibility
- Reinventing the wheel
- PAC format already works

## Consequences

### Positive

- Direct compatibility with `pac solution import --settings-file`
- Simple, readable format
- Git-friendly with deterministic sorting
- Clear sync semantics (preserve values, add new, remove obsolete)

### Negative

- Not compatible with ALM Accelerator pipelines
- No built-in multi-environment support (separate files per environment)

### Neutral

- Users needing ALM Accelerator features must use their tooling
- Format may evolve if Microsoft adds fields (forward-compatible structure)

## References

- [Pre-populate connection references and environment variables](https://learn.microsoft.com/en-us/power-platform/alm/conn-ref-env-variables-build-tools)
- [ALM Accelerator deployment configuration](https://learn.microsoft.com/en-us/power-platform/guidance/alm-accelerator/setup-data-deployment-configuration)
- Extension implementation: `src/shared/domain/entities/DeploymentSettings.ts`
