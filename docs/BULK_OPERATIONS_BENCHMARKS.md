# Bulk Operations Benchmarks

Performance testing for UpsertMultiple operations against Dataverse.

## Test Environment

- **Entity:** `ppds_zipcode` (simple entity with alternate key)
- **Record count:** 42,366
- **Environment:** Developer environment (single tenant)
- **Parallel batches:** 4

## Microsoft's Recommendation

From [Microsoft Docs](https://learn.microsoft.com/en-us/power-apps/developer/data-platform/bulk-operations):

> "Generally, we expect that **100 - 1,000 records per request** is a reasonable place to start if the size of the record data is small and there are no plug-ins."

For elastic tables: **100 records per request** (sent in parallel).

## Test Results

| Test | Approach | Batch Size | Time (s) | Throughput (rec/s) | Notes |
|------|----------|------------|----------|-------------------|-------|
| 1 | Single ServiceClient, Parallel.ForEachAsync | 100 | 933 | 45.4 | Original baseline |
| 2 | Connection Pool (IBulkOperationExecutor) | 1000 | 1223 | 34.6 | 24% slower |
| 3 | Connection Pool (IBulkOperationExecutor) | 100 | TBD | TBD | Isolating batch size variable |

## Variables to Isolate

1. **Batch size** - 100 vs 1000
2. **Connection approach** - Single ServiceClient vs Connection Pool
3. **Affinity cookie** - Enabled (default) vs Disabled

## Observations

### Test 1 (Baseline)
- Single ServiceClient shared across parallel batches
- Affinity cookie enabled (default)
- Batch size 100, 424 batches total
- Consistent ~45/s throughput

### Test 2 (Connection Pool + Large Batch)
- Connection pool with 4 pooled connections
- Affinity cookie disabled
- Batch size 1000, 43 batches total
- First batch took ~2 minutes (cold start)
- Inconsistent batch times (3s to 100+s)
- Overall slower despite "optimizations"

### Test 3 (Connection Pool + Small Batch)
- Pending...
