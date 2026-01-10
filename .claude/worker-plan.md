# Implementation Plan: Issue #276 - Clean up error message duplication in logs

## My Understanding

FaultException<OrganizationServiceFault> errors show the same error message 2-3 times in a single log line due to nested FaultDetail structure. When `_logger.LogError(ex, ...)` is called, the default formatter uses `ex.ToString()` which includes:
1. The top-level exception message
2. "Fault Detail is equal to Exception details: ..."
3. The nested fault message (again)
4. Sometimes another nested layer

Example of current bad output:
```
System.ServiceModel.FaultException`1[Microsoft.Xrm.Sdk.OrganizationServiceFault]: Territory With Ids = 81b3de06... Do Not Exist (Fault Detail is equal to Exception details: ErrorCode: 0x80040217 Message: Territory With Ids = 81b3de06... Do Not Exist TimeStamp: 2026-01-07T15:44:51Z -- Exception details: ErrorCode: 0x80040217 Message: Territory With Ids = 81b3de06... Do Not Exist TimeStamp: ...
```

## Patterns I'll Follow

- **ADR-0020**: Import Error Reporting - error pattern detection and actionable suggestions
- **ADR-0026**: Structured Error Model - user-friendly messages, error codes for programmatic handling
- **ADR-0008**: CLI Output Architecture - stderr for progress/status

## What I'm Implementing

### 1. Add helper method to format clean error from FaultException

In `BulkOperationExecutor.cs`, add a method to extract and format a clean, single-line error message with error code:

```csharp
/// <summary>
/// Formats a clean error message from a FaultException, avoiding duplication.
/// </summary>
/// <returns>Format: "{message} (ErrorCode: 0x{errorCode:X8})"</returns>
private static string FormatCleanFaultMessage(FaultException<OrganizationServiceFault> faultEx)
{
    var fault = faultEx.Detail;
    var message = fault?.Message ?? faultEx.Message;
    var errorCode = fault?.ErrorCode ?? 0;
    return $"{message} (ErrorCode: 0x{errorCode:X8})";
}
```

### 2. Update LogError call to use clean message instead of full exception

At line ~891, change:
```csharp
_logger.LogError(ex, "{Operation} batch failed with non-retryable error. Entity: {Entity}, BatchSize: {BatchSize}",
    operationName, entityLogicalName, batch.Count);
```

To:
```csharp
// Log clean message at Error level, full exception only at Debug for troubleshooting
if (ex is FaultException<OrganizationServiceFault> faultEx)
{
    var cleanMessage = FormatCleanFaultMessage(faultEx);
    _logger.LogError("{Operation} batch failed. Entity: {Entity}, BatchSize: {BatchSize}. {Error}",
        operationName, entityLogicalName, batch.Count, cleanMessage);
    _logger.LogDebug(ex, "{Operation} full exception details for debugging.", operationName);
}
else
{
    _logger.LogError(ex, "{Operation} batch failed with non-retryable error. Entity: {Entity}, BatchSize: {BatchSize}",
        operationName, entityLogicalName, batch.Count);
}
```

### 3. Ensure error messages in BulkOperationError use clean format

The existing `GetExceptionMessage` already extracts `Detail.Message`, which is good. But we should ensure the `ErrorCode` is also formatted nicely when stored.

## What I'm NOT Doing

- **Not modifying ConsoleProgressReporter.cs** - The console reporter uses `MigrationError.Message` which already receives the cleaned message from `GetExceptionMessage()`. The duplication issue is in the ILogger output, not the console progress display.
- **Not changing the error JSONL output** - Full details should remain available in the structured error report as per ADR-0020.
- **Not adding a --debug flag** - MEL's log levels already handle this. Debug-level logging includes full exception.
- **Not modifying PpdsException hierarchy** - This fix is specific to logging of FaultExceptions during bulk operations.

## Files to Modify

- `src/PPDS.Dataverse/BulkOperations/BulkOperationExecutor.cs` - Add clean message formatter and update logging

## Acceptance Criteria Mapping

| Criteria | How Addressed |
|----------|---------------|
| Console shows clean, single error message | Using FormatCleanFaultMessage in LogError |
| Error code included in message | Format includes `(ErrorCode: 0x{code:X8})` |
| Full details available in error JSONL (unchanged) | Not touching error report output |
| --debug flag shows full exception if needed | Using LogDebug for full exception |

## Testing Strategy

1. Build: `dotnet build`
2. Unit tests: `dotnet test --filter "Category!=Integration"`
3. Manual verification would require a Dataverse environment with test data that triggers the "Does Not Exist" error
