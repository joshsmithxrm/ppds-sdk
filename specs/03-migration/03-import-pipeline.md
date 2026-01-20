# PPDS.Migration: Import Pipeline

## Overview

The Import Pipeline processes CMT-compatible data packages and imports them into Dataverse environments with full dependency ordering support. It operates in three sequential phases: tiered entity import, deferred field updates, and many-to-many relationship associations. The pipeline uses bulk APIs for performance, handles circular references through field deferral, and provides real-time progress reporting throughout.

## Public API

### Interfaces

| Interface | Purpose |
|-----------|---------|
| `IImporter` | Main entry point for import operations |
| `IImportPhaseProcessor` | Contract for Phase 2/3 processors |
| `ISchemaValidator` | Validates schema compatibility with target environment |

### Classes

| Class | Purpose |
|-------|---------|
| `TieredImporter` | Primary `IImporter` implementation with three-phase orchestration |
| `DeferredFieldProcessor` | Phase 2: Updates self-referential and circular lookup fields |
| `RelationshipProcessor` | Phase 3: Creates many-to-many associations |
| `BulkOperationProber` | Detects bulk operation support with fallback caching |
| `SchemaValidator` | Validates target schema and handles missing columns |
| `PluginStepManager` | Enables/disables plugin steps during import |

### DTOs/Models

| Type | Purpose |
|------|---------|
| `ImportOptions` | Configuration for import behavior |
| `ImportResult` | Outcome of import with record counts, timing, errors, warnings |
| `EntityImportResult` | Per-entity import outcome |
| `PhaseResult` | Generic outcome for Phase 2/3 |
| `ImportContext` | Shared state passed between phases |
| `ExecutionPlan` | Tiered execution order with deferred fields |
| `ImportTier` | Group of entities that can be imported in parallel |
| `IdMappingCollection` | Thread-safe source→target ID mapping |
| `FieldMetadataCollection` | Target field validity metadata |
| `ImportWarning` | Non-fatal warnings with codes |

## Behaviors

### Normal Operation

1. **Data Loading**: Read CMT-format ZIP via `ICmtDataReader`
2. **Dependency Analysis**: Build graph and execution plan via `IExecutionPlanBuilder`
3. **Schema Validation**: Validate target schema, handle missing columns
4. **Plugin Management**: Optionally disable plugins on configured entities
5. **Phase 1 - Entity Import**: Import records tier-by-tier with ID mapping
6. **Phase 2 - Deferred Fields**: Update self-referential and circular lookups
7. **Phase 3 - M2M Relationships**: Create many-to-many associations
8. **Plugin Restoration**: Re-enable any disabled plugin steps

### Import Phases

```
Data Loading → Dependency Analysis → Schema Validation → Phase 1 (Tiers) → Phase 2 (Deferred) → Phase 3 (M2M)
```

**Phase 1: Tiered Entity Import**
- Process tiers sequentially (Tier 0, then Tier 1, etc.)
- Within each tier, import entities in parallel (configurable: default 4)
- For each entity:
  - Remap entity references using `IdMappingCollection`
  - Null out deferred fields (processed in Phase 2)
  - Apply bypass options (plugins, flows)
  - Execute bulk operations (CreateMultiple/UpdateMultiple/UpsertMultiple)
  - Track source→target ID mappings
- Output: Populated `IdMappingCollection` for later phases

**Phase 2: Deferred Field Updates**
- Triggered when `ExecutionPlan.DeferredFields` is not empty
- For each record with deferred fields:
  - Check ID mapping exists (skip if Phase 1 failed)
  - For each deferred lookup field:
    - Look up mapped target ID
    - Create update with only deferred fields
  - Execute bulk updates
- Handles self-referential lookups (e.g., `account.parentaccountid`)

**Phase 3: M2M Relationship Associations**
- Triggered when `MigrationData.RelationshipData` is not empty
- Pre-load relationship metadata (intersect entity → SchemaName)
- Parallel execution of `AssociateRequest` operations
- Duplicate associations treated as idempotent success (error 0x80040237)

### Lifecycle

- **Initialization**: `TieredImporter` constructed via DI with connection pool, bulk executor, schema reader
- **Operation**: `ImportAsync` orchestrates three phases sequentially
- **Cleanup**: Plugin steps re-enabled in finally block, pooled connections returned automatically

## Edge Cases

| Scenario | Behavior | Notes |
|----------|----------|-------|
| Empty data (no records) | Returns success with zero records imported | Valid but unusual |
| Entity not in target | Depends on `SkipMissingColumns` setting | Schema validation catches |
| Missing column in target | Throws `SchemaMismatchException` or skips | Based on `SkipMissingColumns` |
| Bulk API not supported | Auto-fallback to individual operations | Cached for session |
| Circular reference | Fields deferred to Phase 2 | Breaks dependency cycle |
| Self-referential lookup | Always deferred | e.g., `parentaccountid` |
| Duplicate M2M association | Counted as success | Idempotent - desired state achieved |
| User/team reference not mapped | Uses `CurrentUserId` fallback or keeps original | Based on `UseCurrentUserAsDefault` |
| Record fails in Phase 1 | Skipped in Phase 2/3 | ID mapping won't exist |
| Plugin re-enable fails | Logged as warning only | Import still succeeds |

## Error Handling

| Exception | Condition | Recovery |
|-----------|-----------|----------|
| `SchemaMismatchException` | Target missing required columns | Set `SkipMissingColumns = true` or fix schema |
| `FaultException` | Dataverse operation failure | Depends on `ContinueOnError` |
| `InvalidOperationException` | Data file not found or invalid | Validate data path before import |
| `OperationCanceledException` | Cancellation token triggered | Clean cancellation, partial result |

**Error Strategy:**
- `ContinueOnError = true` (default): Collect errors, continue processing
- `ContinueOnError = false`: Throw immediately, stop import
- Errors collected in thread-safe `ConcurrentBag<MigrationError>`
- `ErrorCallback` option for real-time error streaming
- All errors sanitized (no connection strings or PII)

**Warning Codes:**
- `BULK_NOT_SUPPORTED` - Entity doesn't support bulk operations
- `COLUMN_SKIPPED` - Column missing in target (when SkipMissingColumns = true)
- `SCHEMA_MISMATCH` - Schema differences detected
- `USER_MAPPING_FALLBACK` - User reference resolved via fallback
- `PLUGIN_REENABLE_FAILED` - Failed to re-enable plugin step

## Dependencies

- **Internal**:
  - `PPDS.Dataverse` - `IDataverseConnectionPool`, `IBulkOperationExecutor`
  - `PPDS.Migration.Analysis` - `IDependencyGraphBuilder`, `IExecutionPlanBuilder`
  - `PPDS.Migration.Formats` - `ICmtDataReader`
  - `PPDS.Migration.Models` - All data models
  - `PPDS.Migration.Progress` - Progress reporting infrastructure
- **External**:
  - `Microsoft.PowerPlatform.Dataverse.Client` - `ServiceClient`
  - `Microsoft.Xrm.Sdk` - `Entity`, `EntityReference`, requests

## Configuration

| Setting | Type | Default | Description |
|---------|------|---------|-------------|
| `UseBulkApis` | bool | true | Use CreateMultiple/UpdateMultiple/UpsertMultiple |
| `Mode` | ImportMode | Upsert | Create, Update, or Upsert |
| `ContinueOnError` | bool | true | Continue on individual record failures |
| `MaxParallelEntities` | int | 4 | Max parallel entities within a tier |
| `BypassCustomPlugins` | PluginBypassMode | None | None, Synchronous, Asynchronous, All |
| `BypassPowerAutomateFlows` | bool | false | Bypass Power Automate triggers |
| `SuppressDuplicateDetection` | bool | false | Suppress duplicate detection rules |
| `RespectDisablePluginsSetting` | bool | true | Honor schema `disableplugins=true` |
| `StripOwnerFields` | bool | false | Remove owner/created-by fields |
| `SkipMissingColumns` | bool | false | Skip columns missing in target |
| `UserMappings` | UserMappingCollection | null | User reference remapping |
| `CurrentUserId` | Guid? | null | Fallback user for unmapped references |
| `ErrorCallback` | Action | null | Real-time error streaming |

**Import Modes:**
- `Create` - Insert new records only
- `Update` - Update existing records only
- `Upsert` - Insert or update based on primary key

**CLI Arguments:**
```
ppds data import
  --data <path>                # Required: Data file path
  --profile <name>             # Connection profile
  --environment <url>          # Environment URL
  --mode <mode>                # Create/Update/Upsert
  --parallel <n>               # Max parallel entities
  --continue-on-error          # Continue after failures
  --skip-missing-columns       # Skip columns not in target
  --bypass-plugins             # Bypass custom plugins
  --bypass-flows               # Bypass Power Automate
```

## Thread Safety

- **`TieredImporter`**: Thread-safe for concurrent calls to `ImportAsync`
- **`IdMappingCollection`**: Thread-safe concurrent dictionary for parallel access
- **Error collection**: Uses `ConcurrentBag<MigrationError>`
- **Entity results**: Uses `ConcurrentBag<EntityImportResult>`
- **BulkOperationProber cache**: Thread-safe HashSet with locking

**Concurrency Model:**
- Tiers processed sequentially
- Entities within tier processed in parallel (up to `MaxParallelEntities`)
- Phase 2 entities processed sequentially
- Phase 3 M2M associations processed in parallel (pool-limited)

## Bulk Operation Handling

### Probing Strategy

The `BulkOperationProber` handles entities that don't support bulk operations:

1. Check cached "unsupported" set for entity
2. If cached, immediately fallback to individual operations
3. If not cached, probe with first record only
4. If probe fails with "not supported" pattern:
   - Cache entity name for session
   - Fallback to individual operations for remaining records
5. Merge probe result with batch results (adjust error indices)

**Known Unsupported Entities:** `team`, `queue`

**Error Detection Patterns:**
- "is not enabled on the entity"
- "does not support entities of type"

### Bulk vs Individual Performance

| Approach | Throughput | Notes |
|----------|------------|-------|
| Bulk APIs (CreateMultiple, etc.) | 5x faster | Default, preferred |
| ExecuteMultiple | Baseline | Legacy approach |
| Individual operations | Slowest | Fallback only |

## ID Mapping & Reference Resolution

### IdMappingCollection Operations

| Method | Purpose |
|--------|---------|
| `AddMapping(entity, oldId, newId)` | Record mapping during Phase 1 |
| `TryGetNewId(entity, oldId, out newId)` | Lookup with optional return |
| `GetNewId(entity, oldId)` | Lookup with exception on failure |
| `GetMappingCount(entity)` | Count per entity |
| `TotalMappingCount` | Total across all entities |

### Reference Remapping Logic

**For user/team references:**
1. Check `UserMappings` for explicit mapping
2. If not found and `UseCurrentUserAsDefault = true`:
   - Use `CurrentUserId` as fallback
3. Otherwise return unchanged

**For other entity references:**
1. Check `IdMappings` via `TryGetNewId()`
2. If mapped, return new reference
3. If not mapped, return original (processed in Phase 2)

### Record Preparation

1. Create new Entity object
2. Preserve original ID (required for UpsertMultiple)
3. For UpsertMultiple: Add primary key as attribute
4. For each field:
   - Skip deferred fields
   - Skip owner fields if `StripOwnerFields = true`
   - Skip fields not valid for operation mode
   - Remap EntityReference attributes
5. Special: Force `team.isdefault = false` (prevents conflicts)

## Progress Reporting

### Migration Phases

| Phase | Value | Description |
|-------|-------|-------------|
| Analyzing | 0 | Building dependency graph |
| Importing | 2 | Phase 1: Tier processing |
| ProcessingDeferredFields | 3 | Phase 2: Self-references |
| ProcessingRelationships | 4 | Phase 3: M2M associations |
| Complete | 5 | Import finished |
| Error | 6 | Fatal error occurred |

### Progress Event Details

| Property | Type | Description |
|----------|------|-------------|
| Phase | MigrationPhase | Current phase |
| Entity | string | Current entity |
| TierNumber | int | Current tier (Phase 1) |
| TotalTiers | int | Total tier count |
| Current | int | Records in current batch |
| Total | int | Total records in entity |
| OverallProcessed | int | All records across entities |
| OverallTotal | int | Total record count |
| SuccessCount | int | Successful operations |
| FailureCount | int | Failed operations |
| RecordsPerSecond | double | Current throughput |
| EstimatedRemaining | TimeSpan | Projected completion |
| PercentComplete | double | 0-100 progress |

## Related

- [ADR-0002: Connection Pooling](../docs/adr/002-connection-pooling.md)
- [ADR-0005: DOP-Based Parallelism](../docs/adr/005-dop-based-parallelism.md)
- [ADR-0015: Application Service Layer](../docs/adr/015-application-service-layer.md)
- [Spec: Connection Pooling](../specs/01-dataverse/01-connection-pooling.md)
- [Spec: Bulk Operations](../specs/01-dataverse/02-bulk-operations.md)
- [Spec: Dependency Analysis](../specs/03-migration/01-dependency-analysis.md)
- [Spec: Export Pipeline](../specs/03-migration/02-export-pipeline.md)

## Source Files

| File | Purpose |
|------|---------|
| `src/PPDS.Migration/Import/IImporter.cs` | Main import interface |
| `src/PPDS.Migration/Import/TieredImporter.cs` | Three-phase orchestrator |
| `src/PPDS.Migration/Import/ImportOptions.cs` | Import configuration |
| `src/PPDS.Migration/Import/ImportResult.cs` | Import result models |
| `src/PPDS.Migration/Import/ImportContext.cs` | Shared state between phases |
| `src/PPDS.Migration/Import/IImportPhaseProcessor.cs` | Phase processor interface |
| `src/PPDS.Migration/Import/DeferredFieldProcessor.cs` | Phase 2 implementation |
| `src/PPDS.Migration/Import/RelationshipProcessor.cs` | Phase 3 implementation |
| `src/PPDS.Migration/Import/BulkOperationProber.cs` | Bulk API fallback logic |
| `src/PPDS.Migration/Import/ISchemaValidator.cs` | Schema validation interface |
| `src/PPDS.Migration/Import/SchemaValidator.cs` | Schema validation implementation |
| `src/PPDS.Migration/Import/FieldMetadataCollection.cs` | Target field metadata |
| `src/PPDS.Migration/Import/PluginStepManager.cs` | Plugin enable/disable |
| `src/PPDS.Migration/Models/ExecutionPlan.cs` | Tier and deferred field structure |
| `src/PPDS.Migration/Models/IdMapping.cs` | ID mapping collection |
| `src/PPDS.Migration/Models/MigrationData.cs` | Import data container |
| `src/PPDS.Migration/Analysis/IExecutionPlanBuilder.cs` | Plan builder interface |
| `src/PPDS.Migration/Analysis/ExecutionPlanBuilder.cs` | Plan builder implementation |
| `src/PPDS.Migration/Progress/MigrationPhase.cs` | Phase enumeration |
| `src/PPDS.Migration/Progress/MigrationError.cs` | Error model |
| `src/PPDS.Migration/Progress/ImportWarning.cs` | Warning codes and model |
| `src/PPDS.Migration/Progress/ProgressEventArgs.cs` | Progress event details |
| `src/PPDS.Migration/DependencyInjection/ServiceCollectionExtensions.cs` | DI registration |
| `src/PPDS.Cli/Commands/Data/ImportCommand.cs` | CLI command |
| `tests/PPDS.Migration.Tests/Import/ImportOptionsTests.cs` | Options unit tests |
| `tests/PPDS.Migration.Tests/Import/ImportResultTests.cs` | Result unit tests |
