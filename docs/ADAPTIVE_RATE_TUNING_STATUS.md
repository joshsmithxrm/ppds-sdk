# Adaptive Rate Controller Tuning Status

**Date:** 2024-12-24
**Branch:** `feature/v2-alpha`
**Status:** Implementation Complete - Pending Final Validation

---

## Executive Summary

Successfully implemented **execution time-aware rate control** that:
- **Eliminates throttle cascades** for updates (866 throttles → 0)
- **Doubles throughput** for updates (79/s → 178/s) and deletes (52/s → 102/s)
- **Preserves full speed** for creates via slow batch threshold (~513/s)

---

## Current Implementation

### Components

1. **AdaptiveRateController** - AIMD-based parallelism control
   - Floor = `x-ms-dop-hint × connections` (server recommendation)
   - Ceiling = `min(HardCeiling × connections, ThrottleCeiling, ExecutionTimeCeiling)`
   - Increment by floor on success, 50% decrease on throttle
   - Throttle ceiling from Retry-After duration

2. **Execution Time Ceiling** (NEW)
   - Tracks batch execution times via exponential moving average (EMA)
   - Calculates ceiling: `Factor / avgBatchTimeSeconds`
   - Factor = 250 (tuned for execution time budget of 4s/s)
   - Only applied when batches exceed slow threshold (10s)

3. **Dynamic Parallel Executor**
   - Queries `GetParallelism()` before each batch dispatch
   - Records batch duration after completion via `RecordBatchDuration()`
   - Uses `Task.WhenAny` loop for real-time adjustment

4. **Pre-Flight Guard**
   - Checks `IsThrottled()` before executing requests
   - Returns throttled connections to pool

### Key Configuration (AdaptiveRateOptions)

| Option | Default | Purpose |
|--------|---------|---------|
| `ExecutionTimeCeilingEnabled` | `true` | Enable execution time-based ceiling |
| `ExecutionTimeCeilingFactor` | `250` | Ceiling = Factor / batchTimeSeconds |
| `SlowBatchThresholdMs` | `10000` | Only apply ceiling for batches > 10s |
| `MinBatchSamplesForCeiling` | `3` | Samples before ceiling activates |
| `BatchDurationSmoothingFactor` | `0.3` | EMA weight for recent batches |

---

## Benchmark Results

### Test Configuration
- **Records:** 42,366 ZIP codes
- **Batch Size:** 100 (424 batches)
- **Connections:** 2 (Primary + Secondary, different service principals)
- **Recommended Parallelism:** 5 per connection (floor = 10)

### Final Results (Execution Time Ceiling + Slow Batch Threshold)

| Operation | Duration | Throughput | Throttles | Max Parallelism | Notes |
|-----------|----------|------------|-----------|-----------------|-------|
| **Create** | ~82s | ~513/s | 0 | 104 | Ceiling not applied (batches < 10s) |
| **Update** | 238s | 178/s | 0 | 23 | Ceiling applied at ~20-24 |
| **Delete** | 413s | 102/s | Few, short | 10-28 | Ceiling kicked in as batches slowed |

### Comparison to Baseline

| Operation | Before (No Ceiling) | After (With Ceiling) | Improvement |
|-----------|--------------------:|---------------------:|------------:|
| **Create** | 513/s, 0 throttles | ~513/s, 0 throttles | Same |
| **Update** | 79/s, 866 throttles | 178/s, 0 throttles | **+125%, no throttles** |
| **Delete** | 52/s, 394 throttles | 102/s, few throttles | **+97%, short waits** |

---

## The Problem We Solved

### Root Cause: Execution Time Budget Exhaustion

Microsoft's service protection limits include **1200 seconds execution time per 5-minute window** (= 4s/s budget).

Different operations consume this budget differently:
- **Creates:** Fast server-side (~1-2s), low execution time consumption
- **Updates/Deletes:** Slow server-side (~5-10s), high execution time consumption

When we ramped to high parallelism (80-104) with slow operations:
1. Execution time budget exhausted rapidly
2. Throttle triggered with 80-100 requests in-flight
3. All in-flight requests returned throttle errors (cascade)
4. Retry-After escalated: 1:00 → 3:28 → 6:06 → 8:18

### The Solution: Execution Time-Aware Ceiling

```
ceiling = ExecutionTimeCeilingFactor / averageBatchTimeSeconds
        = 250 / 12s (for updates)
        = ~20 parallelism
```

This keeps execution time consumption within budget:
- 20 concurrent requests completing at 2/second
- ~5s server time each = 10s/s execution time
- With 2 users = 5s/s per user, close to 4s/s budget

### Slow Batch Threshold

To avoid penalizing fast operations (creates), the ceiling only applies when:
```csharp
state.BatchDurationEmaMs >= _options.SlowBatchThresholdMs  // 10 seconds
```

This gives us the best of both worlds:
- Creates (8s batches): No ceiling → 104 parallelism → 513/s
- Updates (10-15s batches): Ceiling applied → 20-23 parallelism → 178/s
- Deletes (variable): Ceiling kicks in when batches slow past 10s

---

## Implementation Details

### Files Modified

| File | Changes |
|------|---------|
| `AdaptiveRateOptions.cs` | Added execution time ceiling options |
| `AdaptiveRateController.cs` | Added `RecordBatchDuration()`, ceiling calculation, slow batch threshold |
| `AdaptiveRateStatistics.cs` | Added `ExecutionTimeCeiling`, `AverageBatchDuration`, `BatchDurationSampleCount` |
| `IAdaptiveRateController.cs` | Added `RecordBatchDuration()` interface method |
| `BulkOperationExecutor.cs` | Track batch duration, call `RecordBatchDuration()` |

### Algorithm

```csharp
// 1. Track batch durations (EMA)
void RecordBatchDuration(connectionName, duration)
{
    ema = alpha * duration + (1 - alpha) * ema;  // alpha = 0.3

    if (samples >= MinBatchSamplesForCeiling)
    {
        executionTimeCeiling = Factor / (ema / 1000);  // 250 / seconds
    }
}

// 2. Apply ceiling only for slow batches
int GetParallelism(...)
{
    ceiling = hardCeiling;

    if (throttleCeiling active)
        ceiling = min(ceiling, throttleCeiling);

    // Only apply for slow batches (>10s)
    if (batchDurationEma >= SlowBatchThresholdMs)
        ceiling = min(ceiling, executionTimeCeiling);

    return currentParallelism;  // capped at ceiling
}
```

---

## Tuning Guidelines

### When to Adjust ExecutionTimeCeilingFactor

| Symptom | Adjustment |
|---------|------------|
| Still getting throttle cascades | Lower factor (e.g., 200) |
| Throughput too conservative | Raise factor (e.g., 300) |
| Creates being capped | Raise SlowBatchThresholdMs (e.g., 12000) |

### Relationship Between Settings

```
Higher Factor (300-400) → Higher ceiling → More aggressive → Risk of throttles
Lower Factor (150-250) → Lower ceiling → More conservative → Safer but slower

Higher SlowBatchThresholdMs (12s+) → More operations skip ceiling → Faster but riskier
Lower SlowBatchThresholdMs (8s) → More operations get ceiling → Safer
```

---

## Log Analysis Reference

### Identifying Execution Time Throttles

Error code `-2147015903` indicates execution time limit exceeded:
```
Service protection limit hit. ErrorCode: -2147015903, RetryAfter: 00:01:00
```

### Reading Execution Time Ceiling Logs

```
Connection Primary: Execution time ceiling updated to 24 (avg batch: 10.3s, samples: 50)
                                                    ↑            ↑              ↑
                                        Calculated ceiling    EMA of batches   Sample count
```

### Ceiling Taking Effect

```
UpsertMultiple: Parallelism changed 20 → 23. InFlight: 19, Pending: 393
                                      ↑
                        Capped at execution time ceiling (was targeting 30)
```

---

## Known Limitations

1. **EMA Lag:** Ceiling based on moving average can lag behind sudden batch time changes. Deletes that start fast (7s) then slow down (22s) may overshoot before ceiling adjusts.

2. **Per-Connection Tracking:** Each connection tracks its own batch times. If load is uneven across connections, ceilings may differ.

3. **No Operation Type Awareness:** Ceiling doesn't know if operation is create/update/delete - relies on batch timing to infer.

---

## Next Steps

1. **Final Validation:** Run all three operations (create, update, delete) with slow batch threshold
2. **Production Testing:** Validate with larger datasets and different environments
3. **Documentation:** Update CLAUDE.md and API documentation with new options
4. **Consider:** Add per-operation ceiling overrides if needed

---

## References

- [Optimize performance for bulk operations](https://learn.microsoft.com/en-us/power-apps/developer/data-platform/optimize-performance-create-update)
- [Service protection API limits](https://learn.microsoft.com/en-us/power-apps/developer/data-platform/api-limits)
- ADR-0004: Adaptive Rate Control (in `docs/adr/`)
