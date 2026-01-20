# PPDS.Dataverse: Bulk Operations

## Overview

The Bulk Operations subsystem provides high-performance CRUD operations using Dataverse's modern bulk APIs (CreateMultiple, UpdateMultiple, UpsertMultiple, DeleteMultiple). It manages batching, parallelism, throttle handling, and error recovery, enabling efficient data migration and synchronization while respecting Microsoft's service protection limits.

## Public API

### Interfaces

| Interface | Purpose |
|-----------|---------|
| `IBulkOperationExecutor` | Executes bulk CRUD operations with batching and parallelism |

### Classes

| Class | Purpose |
|-------|---------|
| `BulkOperationExecutor` | Implementation using connection pool and modern bulk APIs |
| `BatchParallelismCoordinator` | Coordinates batch parallelism across concurrent bulk operations |

### DTOs/Models

| Type | Purpose |
|------|---------|
| `BulkOperationOptions` | Configuration (batch size, bypass options, parallelism) |
| `BulkOperationResult` | Result of an operation (success/failure counts, errors) |
| `BulkOperationError` | Error details for a failed record |
| `BatchFailureDiagnostic` | Diagnostic info identifying which record caused batch failure |
| `CustomLogicBypass` | Flags for bypassing plugins/workflows during bulk operations |

### Exceptions

| Type | Purpose |
|------|---------|
| `BatchCoordinatorExhaustedException` | Thrown when batch coordinator cannot acquire slot within timeout |

## Behaviors

### Normal Operation

1. **Preparation**: Entities/IDs collected into batches of configurable size (default 100)
2. **Parallelism Selection**: Uses `pool.GetTotalRecommendedParallelism()` or fixed override
3. **Execution**:
   - Sequential: Single batch or parallelism=1
   - Parallel: Uses `Parallel.ForEachAsync` with `BatchParallelismCoordinator` slot acquisition
4. **Batch Processing**: Each batch:
   - Acquires connection from pool
   - Pre-flight checks for throttle state
   - Executes bulk API request (CreateMultiple, UpdateMultiple, etc.)
   - Returns connection to pool
5. **Result Aggregation**: Thread-safe collection of success/failure counts and errors

### Supported Operations

| Operation | API Used | Notes |
|-----------|----------|-------|
| CreateMultiple | `CreateMultipleRequest` | Returns created record IDs |
| UpdateMultiple | `UpdateMultipleRequest` | All-or-nothing per batch (standard tables) |
| UpsertMultiple | `UpsertMultipleRequest` | Returns created/updated counts |
| DeleteMultiple (elastic) | `DeleteMultiple` custom action | Partial success supported |
| DeleteMultiple (standard) | `ExecuteMultipleRequest` with `DeleteRequest` | Uses ContinueOnError |

### Error Handling Strategy

The executor uses a layered retry approach:

| Error Type | Behavior | Retries |
|------------|----------|---------|
| Service Protection (429) | Wait and retry; pool handles connection selection | Infinite |
| Pool Exhausted | Exponential backoff (1s-32s) | Infinite |
| Auth Failure | Mark connection invalid, invalidate seed if token issue | Limited (MaxConnectionRetries) |
| Connection Failure | Mark connection invalid, retry with new connection | Limited (MaxConnectionRetries) |
| TVP Race Condition | Exponential backoff (500ms-2s) | 3 attempts |
| SQL Deadlock | Exponential backoff (500ms-2s) | 3 attempts |
| Cancellation | Return empty result (not failure) | None |
| Other Errors | Convert to failure result | None |

### Lifecycle

- **Initialization**: Executor receives connection pool via DI
- **Operation**: Each bulk call is independent; connections borrowed and returned per batch
- **Cleanup**: No persistent resources; pool manages connection lifecycle

## Edge Cases

| Scenario | Behavior | Notes |
|----------|----------|-------|
| Empty input | Returns success with 0 counts | No API calls made |
| Single record | Single batch, sequential execution | Efficient for small operations |
| All throttled | Waits for non-throttled connection | Pool handles wait logic |
| Partial failure (elastic) | Extracts `Plugin.BulkApiErrorDetails` | Returns mixed result |
| Batch failure (standard) | All records in batch marked failed | All-or-nothing semantics |
| Self-reference in batch | Diagnostic identifies problematic record | Pattern: `SELF_REFERENCE` |
| Same-batch reference | Diagnostic identifies dependency | Pattern: `SAME_BATCH_REFERENCE` |
| Missing lookup target | Diagnostic with field name and reference | Pattern: `MISSING_REFERENCE` |

## Error Handling

| Exception | Condition | Recovery |
|-----------|-----------|----------|
| `PoolExhaustedException` | No connections available | Retry with exponential backoff (infinite) |
| `DataverseConnectionException` | Auth/connection failure after retries | Propagates with connection info |
| `FaultException<OrganizationServiceFault>` | Dataverse business logic error | Converted to `BulkOperationError` |
| `BatchCoordinatorExhaustedException` | Batch slot timeout | Reduce `MaxParallelEntities` |
| `OperationCanceledException` | User cancellation | Returns empty result |

## Dependencies

- **Internal**:
  - `PPDS.Dataverse.Pooling.IDataverseConnectionPool` - Connection management
  - `PPDS.Dataverse.Resilience.IThrottleTracker` - Pre-flight throttle checks
  - `PPDS.Dataverse.Progress.ProgressTracker` - Progress reporting
  - `PPDS.Dataverse.DependencyInjection.DataverseOptions` - Configuration
- **External**:
  - `Microsoft.Xrm.Sdk` (OrganizationRequest, Entity, etc.)
  - `Microsoft.Xrm.Sdk.Messages` (CreateMultipleRequest, etc.)
  - `Microsoft.Extensions.Logging.Abstractions`
  - `Microsoft.Extensions.Options`

## Configuration

### BulkOperationOptions

| Setting | Type | Default | Description |
|---------|------|---------|-------------|
| `BatchSize` | int | 100 | Records per batch (optimal for both standard and elastic) |
| `ElasticTable` | bool | false | Target is Cosmos DB-backed elastic table |
| `ContinueOnError` | bool | true | Continue after record failures (delete on standard tables) |
| `BypassCustomLogic` | enum | None | Bypass sync/async plugins and workflows |
| `BypassPowerAutomateFlows` | bool | false | Bypass Power Automate triggers |
| `SuppressDuplicateDetection` | bool | false | Skip duplicate detection rules |
| `Tag` | string? | null | Tag value passed to plugin execution context |
| `MaxParallelBatches` | int? | null | Override parallelism (null = use pool DOP) |

### CustomLogicBypass Flags

| Flag | Dataverse Parameter | Effect |
|------|---------------------|--------|
| `None` | (none) | Execute all custom logic |
| `Synchronous` | `CustomSync` | Bypass sync plugins and workflows |
| `Asynchronous` | `CustomAsync` | Bypass async plugins and workflows |
| `All` | `CustomSync,CustomAsync` | Bypass all custom logic |

### Request Parameters Applied

| Parameter | Condition | Purpose |
|-----------|-----------|---------|
| `BypassBusinessLogicExecution` | When `BypassCustomLogic != None` | Skip custom plugins/workflows |
| `SuppressCallbackRegistrationExpanderJob` | When `BypassPowerAutomateFlows` | Skip Power Automate flows |
| `SuppressDuplicateDetection` | When `SuppressDuplicateDetection` | Skip duplicate detection |
| `tag` | When `Tag` is set | Pass to plugin context's SharedVariables |

## Thread Safety

- **Thread-safe members**: All methods on `IBulkOperationExecutor`
- **Aggregation strategy**:
  - `Interlocked.Add` for success/failure counters
  - `ConcurrentBag<T>` for error and ID collection
  - `Interlocked.Exchange` for flag updates
- **Connection handling**: Each batch acquires its own connection; pool manages concurrency
- **Guarantees**: Multiple bulk operations can execute concurrently safely

## Performance Considerations

- **Batch size**: 100 is optimal (benchmarked for both standard and elastic tables)
- **CreateMultiple vs ExecuteMultiple**: CreateMultiple is ~5x faster than ExecuteMultiple with individual creates
- **DOP-based parallelism**: Uses Microsoft's `x-ms-dop-hint` header for optimal concurrency
- **Pre-flight throttle check**: Avoids "in-flight avalanche" where many requests hit throttled connection simultaneously
- **Affinity cookie**: Disabled by pool for better load distribution (critical for bulk performance)
- **Batch coordinator**: Prevents over-subscription when multiple entities import in parallel

## Batch Failure Diagnostics

When a batch fails with a "Does Not Exist" error, the executor scans all records to identify the problematic reference:

| Pattern | Description | Suggestion |
|---------|-------------|------------|
| `SELF_REFERENCE` | Record references itself | Two-pass import: create first, update self-references second |
| `SAME_BATCH_REFERENCE` | References another record in same batch | Dependency-aware batching |
| `MISSING_REFERENCE` | References record not in target | Import referenced entity first |

## Related

- [ADR-0002: Multi-Connection Pooling](../../docs/adr/0002_MULTI_CONNECTION_POOLING.md)
- [ADR-0005: DOP-Based Parallelism](../../docs/adr/0005_DOP_BASED_PARALLELISM.md)
- [ADR-0019: Pool-Managed Concurrency](../../docs/adr/0019_POOL_MANAGED_CONCURRENCY.md)
- [Pattern: Bulk Operations](../../docs/patterns/bulk-operations.cs)

## Source Files

| File | Purpose |
|------|---------|
| `src/PPDS.Dataverse/BulkOperations/IBulkOperationExecutor.cs` | Executor interface |
| `src/PPDS.Dataverse/BulkOperations/BulkOperationExecutor.cs` | Executor implementation |
| `src/PPDS.Dataverse/BulkOperations/BulkOperationOptions.cs` | Configuration options |
| `src/PPDS.Dataverse/BulkOperations/BulkOperationResult.cs` | Result and error DTOs |
| `src/PPDS.Dataverse/BulkOperations/BatchFailureDiagnostic.cs` | Failure diagnostic DTO |
| `src/PPDS.Dataverse/BulkOperations/CustomLogicBypass.cs` | Bypass flags enum |
| `src/PPDS.Dataverse/BulkOperations/BatchParallelismCoordinator.cs` | Batch coordination |
| `tests/PPDS.Dataverse.Tests/BulkOperations/BulkOperationResultTests.cs` | Result unit tests |
| `tests/PPDS.Dataverse.Tests/BulkOperations/BulkOperationOptionsTests.cs` | Options unit tests |
| `tests/PPDS.Dataverse.Tests/BulkOperations/CustomLogicBypassTests.cs` | CustomLogicBypass tests |
| `tests/PPDS.Dataverse.Tests/BulkOperations/BatchFailureDiagnosticTests.cs` | Diagnostic tests |
| `tests/PPDS.Dataverse.Tests/BulkOperations/BatchParallelismCoordinatorTests.cs` | Coordinator tests |
| `tests/PPDS.LiveTests/BulkOperations/BulkOperationLiveTests.cs` | Integration tests |
