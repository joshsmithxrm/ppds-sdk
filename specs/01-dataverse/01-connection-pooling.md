# PPDS.Dataverse: Connection Pooling

## Overview

The Connection Pooling subsystem manages a pool of Dataverse connections with intelligent selection, lifecycle management, and throttle awareness. It supports multiple connection sources (Application Users) to distribute load and multiply API quota, enabling high-throughput data operations while respecting Microsoft's service protection limits.

## Public API

### Interfaces

| Interface | Purpose |
|-----------|---------|
| `IDataverseConnectionPool` | Main pool interface for acquiring/returning connections |
| `IPooledClient` | A client obtained from the pool with lifecycle tracking |
| `IConnectionSource` | Provides seed ServiceClient for cloning; abstracts authentication |
| `IConnectionSelectionStrategy` | Strategy for selecting which connection to use |

### Classes

| Class | Purpose |
|-------|---------|
| `DataverseConnectionPool` | High-performance pool implementation |
| `PooledClient` | Wrapper that returns connection to pool on dispose |
| `ConnectionStringSource` | Creates ServiceClient from connection string configuration |
| `ServiceClientSource` | Wraps pre-authenticated ServiceClient (device code, managed identity) |
| `RoundRobinStrategy` | Simple rotation through connections |
| `LeastConnectionsStrategy` | Selects connection with fewest active clients |
| `ThrottleAwareStrategy` | Avoids throttled connections with round-robin fallback (default) |
| `BatchParallelismCoordinator` | Coordinates batch parallelism across concurrent bulk operations |

### DTOs/Models

| Type | Purpose |
|------|---------|
| `ConnectionPoolOptions` | Pool configuration (timeouts, strategy, validation) |
| `DataverseConnection` | Configuration for a Dataverse connection (credentials, URL) |
| `PoolStatistics` | Statistics and health information for the pool |
| `ConnectionStatistics` | Per-connection statistics |
| `SeedInitializationResult` | Result of initializing a seed connection |
| `SeedFailureReason` | Enum categorizing seed initialization failures |

### Exceptions

| Type | Purpose |
|------|---------|
| `PoolExhaustedException` | Thrown when no connection available within timeout |
| `BatchCoordinatorExhaustedException` | Thrown when batch coordinator cannot acquire slot |

## Behaviors

### Normal Operation

1. **Initialization**: Pool constructor initializes seeds for all connection sources, discovers DOP (degrees of parallelism) from server's `x-ms-dop-hint` header, sizes semaphore to total DOP, warms pool with 1 connection per source
2. **Acquire**: Caller requests client via `GetClientAsync()` or `GetClient()`:
   - Wait for non-throttled connection (if all throttled)
   - Acquire semaphore slot (respects pool capacity)
   - Select connection via configured strategy
   - Return existing pooled client or create new clone
3. **Use**: Caller executes operations; `PooledClient` wraps all calls with `ThrottleDetector` to record throttle events
4. **Return**: Caller disposes `IPooledClient`:
   - Connection marked invalid → disposed immediately
   - Pool full → disposed
   - Otherwise → reset, returned to pool queue, semaphore released

### Lifecycle

- **Initialization**: Seeds created lazily or eagerly based on `Enabled` option; DOP discovered from `ServiceClient.RecommendedDegreesOfParallelism`; warm-up creates 1 connection per source
- **Operation**: Semaphore limits concurrent checkouts; selection strategy picks source; cloning creates pool members from seed; throttle state tracked per connection
- **Cleanup**: `Dispose`/`DisposeAsync` cancels validation loop, disposes all pooled clients, disposes sources (which dispose their seed clients)

### Background Validation

When `EnableValidation` is true (default), a background task periodically:
1. Validates each pooled connection (checks `IsReady`, age, idle time)
2. Evicts invalid connections
3. Warms pool back to 1 connection per source

## Edge Cases

| Scenario | Behavior | Notes |
|----------|----------|-------|
| Empty sources | Throws `ArgumentException` | At least one source required |
| All connections throttled | Waits for shortest expiry | Respects `MaxRetryAfterTolerance` if set |
| Pool exhausted | Throws `PoolExhaustedException` | After `AcquireTimeout` expires |
| Seed creation fails | Records in `InitializationResults` | Uses default DOP=4 for failed source |
| Connection marked invalid | Disposed on return | Not returned to pool |
| Idle connection expired | Evicted during validation | Based on `MaxIdleTime` |
| Connection exceeded lifetime | Evicted during validation | Based on `MaxLifetime` |
| Clone fails during throttle | Throws `DataverseConnectionException` | Clone makes API call that may fail |
| Disposed pool | Throws `ObjectDisposedException` | All operations check disposed state |

## Error Handling

| Exception | Condition | Recovery |
|-----------|-----------|----------|
| `PoolExhaustedException` | No connection within `AcquireTimeout` | Retry after delay; consider increasing pool size or reducing parallelism |
| `TimeoutException` | Connection not available in time | Same as `PoolExhaustedException` |
| `DataverseConnectionException` | Seed/clone creation failed | Check credentials, network; source may continue with default DOP |
| `ServiceProtectionException` | All throttled, wait exceeds tolerance | Reduce request rate; add more Application Users |
| `ObjectDisposedException` | Pool already disposed | Create new pool instance |
| `InvalidOperationException` | Pool not enabled | Check `ConnectionPoolOptions.Enabled` |

## Dependencies

- **Internal**:
  - `PPDS.Dataverse.Client.IDataverseClient` - Client abstraction
  - `PPDS.Dataverse.Resilience.IThrottleTracker` - Throttle state tracking
  - `PPDS.Dataverse.Configuration.*` - Connection string building
  - `PPDS.Dataverse.BulkOperations.BatchParallelismCoordinator` - Batch coordination
- **External**:
  - `Microsoft.PowerPlatform.Dataverse.Client` (ServiceClient)
  - `Microsoft.Extensions.Logging.Abstractions`
  - `Microsoft.Extensions.Options`

## Configuration

| Setting | Type | Default | Description |
|---------|------|---------|-------------|
| `Enabled` | bool | true | Whether pooling is enabled |
| `MaxPoolSize` | int | 0 | Fixed pool size; 0 = use DOP-based sizing |
| `AcquireTimeout` | TimeSpan | 120s | Max wait for connection |
| `MaxIdleTime` | TimeSpan | 5min | Max idle before eviction |
| `MaxLifetime` | TimeSpan | 60min | Max connection lifetime |
| `DisableAffinityCookie` | bool | true | Disable for load distribution (critical for performance) |
| `SelectionStrategy` | enum | ThrottleAware | How to select connections |
| `ValidationInterval` | TimeSpan | 1min | Background validation frequency |
| `EnableValidation` | bool | true | Enable background validation |
| `ValidateOnCheckout` | bool | true | Validate before returning to caller |
| `MaxConnectionRetries` | int | 2 | Retry attempts for auth/connection failures |
| `MaxRetryAfterTolerance` | TimeSpan? | null | Max acceptable Retry-After; null = wait indefinitely |

### Connection Selection Strategies

| Strategy | Behavior |
|----------|----------|
| `RoundRobin` | Simple rotation through all connections |
| `LeastConnections` | Select connection with fewest active clients |
| `ThrottleAware` | Skip throttled connections, round-robin among available (default) |

## Thread Safety

- **Thread-safe members**: All public methods on `IDataverseConnectionPool`
- **Locking strategy**:
  - `SemaphoreSlim` controls concurrent checkouts (sized to total DOP)
  - `ConcurrentDictionary` for pools, active counts, request counts, seed clients
  - `ConcurrentQueue` for each source's pooled connections
  - `object` lock for pool queue synchronization
  - Per-source `SemaphoreSlim` for seed creation
  - Interlocked operations for statistics counters and disposed flag
- **Guarantees**:
  - Multiple consumers can safely share a pool (fair queuing)
  - Seed creation is serialized per-source to prevent races
  - Disposal is idempotent (safe to call multiple times)

## Performance Considerations

- **Connection cloning**: ~42,000x faster than creating new ServiceClient
- **Affinity cookie**: Disabling spreads load across backend nodes (order of magnitude improvement)
- **DOP-based sizing**: Uses Microsoft's `x-ms-dop-hint` (typically 4-52 per user)
- **Multi-user scaling**: N Application Users = N x DOP parallelism, N x 6,000 requests/5min quota
- **Performance settings applied**: `ThreadPool.SetMinThreads(100, 100)`, `ServicePointManager.DefaultConnectionLimit = 65000`

## Related

- [ADR-0002: Multi-Connection Pooling](../../docs/adr/0002_MULTI_CONNECTION_POOLING.md)
- [ADR-0003: Throttle-Aware Connection Selection](../../docs/adr/0003_THROTTLE_AWARE_SELECTION.md)
- [ADR-0005: DOP-Based Parallelism](../../docs/adr/0005_DOP_BASED_PARALLELISM.md)
- [ADR-0006: Connection Source Abstraction](../../docs/adr/0006_CONNECTION_SOURCE_ABSTRACTION.md)
- [ADR-0019: Pool-Managed Concurrency](../../docs/adr/0019_POOL_MANAGED_CONCURRENCY.md)

## Source Files

| File | Purpose |
|------|---------|
| `src/PPDS.Dataverse/Pooling/IDataverseConnectionPool.cs` | Main pool interface |
| `src/PPDS.Dataverse/Pooling/DataverseConnectionPool.cs` | Pool implementation |
| `src/PPDS.Dataverse/Pooling/IPooledClient.cs` | Pooled client interface |
| `src/PPDS.Dataverse/Pooling/PooledClient.cs` | Pooled client implementation |
| `src/PPDS.Dataverse/Pooling/IConnectionSource.cs` | Connection source interface |
| `src/PPDS.Dataverse/Pooling/ConnectionStringSource.cs` | Config-based source |
| `src/PPDS.Dataverse/Pooling/ServiceClientSource.cs` | Pre-authenticated source |
| `src/PPDS.Dataverse/Pooling/ConnectionPoolOptions.cs` | Pool configuration |
| `src/PPDS.Dataverse/Pooling/DataverseConnection.cs` | Connection configuration |
| `src/PPDS.Dataverse/Pooling/PoolStatistics.cs` | Statistics DTOs |
| `src/PPDS.Dataverse/Pooling/PoolExhaustedException.cs` | Pool exhausted exception |
| `src/PPDS.Dataverse/Pooling/SeedInitializationResult.cs` | Seed init result DTO |
| `src/PPDS.Dataverse/Pooling/Strategies/IConnectionSelectionStrategy.cs` | Strategy interface |
| `src/PPDS.Dataverse/Pooling/Strategies/ThrottleAwareStrategy.cs` | Default strategy |
| `src/PPDS.Dataverse/Pooling/Strategies/RoundRobinStrategy.cs` | Round-robin strategy |
| `src/PPDS.Dataverse/Pooling/Strategies/LeastConnectionsStrategy.cs` | Least connections strategy |
| `src/PPDS.Dataverse/BulkOperations/BatchParallelismCoordinator.cs` | Batch coordination |
| `tests/PPDS.Dataverse.Tests/Pooling/DataverseConnectionPoolTests.cs` | Unit tests |
| `tests/PPDS.Dataverse.Tests/Pooling/PoolExhaustedExceptionTests.cs` | Exception tests |
| `tests/PPDS.Dataverse.Tests/Pooling/PoolSizingTests.cs` | Pool sizing tests |
