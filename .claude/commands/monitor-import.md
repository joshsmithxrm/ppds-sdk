# Monitor Import

Monitor a running PPDS data import for progress, errors, throttling, and pool exhaustion.

## Usage

`/monitor-import <log-file-path> [errors-file-path]`

## Arguments

- `log-file-path` (required): Path to the import log file (e.g., `import-log.txt`)
- `errors-file-path` (optional): Path to the `.errors.jsonl` file for detailed error analysis

## What to Monitor

1. **Progress**: Track entity completion rates and throughput (rec/s)
2. **Errors**: Identify error patterns:
   - `POOL_EXHAUSTION` - BatchCoordinatorExhaustedException or PoolExhaustedException
   - `MISSING_REFERENCE` - Entity with ID does not exist
   - `SELF_REFERENCE` - Record references itself
   - `BULK_NOT_SUPPORTED` - UpsertMultiple/CreateMultiple not enabled
   - `MISSING_USER` - systemuser does not exist
3. **Throttling**: Look for "throttled" or "Retry-After" in logs
4. **Pool Health**: Check for connection failures or auth failures

## Monitoring Approach

1. Read the log file periodically (every 30-60 seconds)
2. Parse progress lines: `[entity] X/Y (Z%) @ N rec/s`
3. Count and categorize errors
4. Report summary with:
   - Entities completed vs in-progress
   - Current throughput
   - Error counts by pattern
   - Any warnings (throttling, pool issues)

## Example Output Format

```
=== Import Monitor (12:34:56) ===
Progress:
  - et_country: 246/246 (100%) DONE
  - et_source: 1,500/2,000 (75%) @ 85 rec/s
  - msdyn_postalcode: 50,000/120,000 (42%) @ 1,200 rec/s

Errors (67 total):
  - SELF_REFERENCE: 66 (et_source batch failure)
  - BULK_NOT_SUPPORTED: 1 (team)

Warnings:
  - None

Pool: Healthy (no exhaustion or throttling detected)
```

## When Import Finishes

Provide a final summary:
- Total records imported
- Total failures by pattern
- Duration
- Recommendations for failed records (e.g., "66 et_source records failed due to self-reference - consider two-pass import")

## Instructions for Claude

When this command is invoked:

1. Read the log file at the specified path
2. If errors file provided, also read `.errors.jsonl` for detailed diagnostics
3. Parse and summarize the current state
4. Report progress, errors, and any issues
5. If the user wants continuous monitoring, update every 60 seconds
6. When import completes, provide final analysis with recommendations

Use the Read tool to access the files. Parse log lines for patterns like:
- Progress: `[HH:mm:ss] [entity] N/M (P%) @ R rec/s`
- Errors: Look for "ERROR", "Exception", "failed"
- Completion: "Import completed" or "Import failed"
