# Error Handling

**Status:** Implemented
**Version:** 1.0
**Last Updated:** 2026-01-21
**Code:** [src/PPDS.Cli/Infrastructure/Errors/](../src/PPDS.Cli/Infrastructure/Errors/)

---

## Overview

PPDS uses a structured error model that provides consistent error information across all interfaces (CLI, TUI, RPC, VS Code). The system combines hierarchical error codes for programmatic handling with user-friendly messages for display, while automatically redacting sensitive information like credentials.

### Goals

- **Consistent Error Information**: Same error data available across CLI, TUI, and RPC interfaces
- **Programmatic Handling**: Hierarchical error codes enable retry logic, re-authentication flows, and category-based handling
- **User-Friendly Messages**: Safe-to-display messages that explain problems without leaking technical details
- **Security**: Automatic redaction of credentials from all error messages

### Non-Goals

- Exception translation at the Dataverse SDK level (handled by PPDS.Dataverse resilience layer)
- Logging infrastructure (separate concern, uses standard ILogger)
- User-facing error message localization (future enhancement)

---

## Architecture

```
┌──────────────────────────────────────────────────────────────────────┐
│                        Application Services                          │
│                 (throw PpdsException subclasses)                     │
└──────────────────────────────────────────────────────────────────────┘
                                    │
                                    ▼
┌──────────────────────────────────────────────────────────────────────┐
│                       ExceptionMapper                                │
│    (maps any exception → StructuredError + exit code)                │
└──────────────────────────────────────────────────────────────────────┘
                                    │
            ┌───────────────────────┼───────────────────────┐
            ▼                       ▼                       ▼
    ┌──────────────┐       ┌──────────────┐       ┌──────────────┐
    │     CLI      │       │     TUI      │       │     RPC      │
    │ ErrorOutput  │       │ MessageBox   │       │ RpcException │
    │ + exit code  │       │   Dialog     │       │ + JSON-RPC   │
    └──────────────┘       └──────────────┘       └──────────────┘
```

Application Services throw `PpdsException` (or subclasses) containing both machine-readable error codes and user-friendly messages. The `ExceptionMapper` provides a centralized translation layer that converts any exception type to a `StructuredError` with appropriate error codes and exit codes. Each UI handles the structured error according to its display requirements.

### Components

| Component | Responsibility |
|-----------|----------------|
| [PpdsException](../src/PPDS.Cli/Infrastructure/Errors/PpdsException.cs) | Base exception with ErrorCode, UserMessage, Severity, Context |
| [ErrorCodes](../src/PPDS.Cli/Infrastructure/Errors/ErrorCodes.cs) | Hierarchical error code constants |
| [ExceptionMapper](../src/PPDS.Cli/Infrastructure/Errors/ExceptionMapper.cs) | Maps any exception to StructuredError + exit code |
| [StructuredError](../src/PPDS.Cli/Infrastructure/Errors/StructuredError.cs) | Serializable error container for cross-UI consistency |
| [ExitCodes](../src/PPDS.Cli/Infrastructure/Errors/ExitCodes.cs) | Standard CLI exit codes for scripting |
| [ConnectionStringRedactor](../src/PPDS.Dataverse/Security/ConnectionStringRedactor.cs) | Removes credentials from error messages |

### Dependencies

- Uses patterns from: [architecture.md](./architecture.md)
- Related to: [application-services.md](./application-services.md) (services throw PpdsException)
- Related to: [cli.md](./cli.md) (CLI error output)

---

## Specification

### Core Requirements

1. All Application Services MUST throw `PpdsException` or subclasses, never raw exceptions
2. Every `PpdsException` MUST have an `ErrorCode` from the `ErrorCodes` class
3. Every `PpdsException` MUST have a `UserMessage` safe for display to end users
4. Sensitive data (credentials, tokens) MUST be automatically redacted from error messages
5. CLI commands MUST return appropriate exit codes for scripting integration

### Exception Hierarchy

```
Exception
├── PpdsException                    # Base: ErrorCode, UserMessage, Severity, Context
│   ├── PpdsAuthException            # RequiresReauthentication flag
│   ├── PpdsThrottleException        # RetryAfter timespan
│   ├── PpdsValidationException      # List of ValidationError records
│   └── PpdsNotFoundException        # ResourceType, ResourceId
│
├── AuthenticationException          # PPDS.Auth: credential acquisition failures
├── DataverseAuthenticationException # PPDS.Dataverse: mid-operation auth failures
├── DataverseConnectionException     # PPDS.Dataverse: connection establishment failures
├── ServiceProtectionException       # PPDS.Dataverse: throttling (429 responses)
└── ConfigurationException           # PPDS.Dataverse: invalid configuration
```

### Error Flow

**Service Throwing:**

1. **Catch lower-level exception**: Service catches SDK/framework exceptions
2. **Map to PPDS exception**: Wrap in appropriate `PpdsException` subclass
3. **Set error code**: Use constant from `ErrorCodes` class
4. **Set user message**: Write human-readable message without technical jargon
5. **Throw**: Let exception propagate to UI layer

**UI Handling:**

1. **Catch PpdsException**: UI catches at command/handler level
2. **Map to StructuredError**: Use `ExceptionMapper.Map()` for consistent transformation
3. **Display appropriately**: CLI uses ErrorOutput, TUI uses MessageBox, RPC uses RpcException
4. **Return exit code**: CLI uses `ExceptionMapper.ToExitCode()` for process exit

### Constraints

- `UserMessage` MUST NOT contain stack traces, GUIDs, or internal implementation details
- Error codes MUST follow the `Category.Subcategory` hierarchical format
- Services MUST NOT let `FaultException`, `HttpRequestException`, or similar leak to UI layer

---

## Core Types

### PpdsException

Base exception for all Application Service errors ([`PpdsException.cs:20-63`](../src/PPDS.Cli/Infrastructure/Errors/PpdsException.cs#L20-L63)).

```csharp
public class PpdsException : Exception
{
    public string ErrorCode { get; init; }      // Machine-readable code
    public string UserMessage { get; init; }    // Safe for display
    public PpdsSeverity Severity { get; init; } // Info, Warning, Error
    public IDictionary<string, object>? Context { get; init; } // Debug data
}
```

### Specialized Exceptions

**PpdsAuthException** ([`PpdsException.cs:83-97`](../src/PPDS.Cli/Infrastructure/Errors/PpdsException.cs#L83-L97)) - Authentication failures with re-auth flag:

```csharp
public class PpdsAuthException : PpdsException
{
    public bool RequiresReauthentication { get; init; }
}
```

**PpdsThrottleException** ([`PpdsException.cs:102-117`](../src/PPDS.Cli/Infrastructure/Errors/PpdsException.cs#L102-L117)) - Rate limiting with retry delay:

```csharp
public class PpdsThrottleException : PpdsException
{
    public TimeSpan RetryAfter { get; init; }
}
```

**PpdsValidationException** ([`PpdsException.cs:122-149`](../src/PPDS.Cli/Infrastructure/Errors/PpdsException.cs#L122-L149)) - Input validation with field-level errors:

```csharp
public class PpdsValidationException : PpdsException
{
    public IReadOnlyList<ValidationError> Errors { get; init; }
}

public record ValidationError(string Field, string Message);
```

**PpdsNotFoundException** ([`PpdsException.cs:161-182`](../src/PPDS.Cli/Infrastructure/Errors/PpdsException.cs#L161-L182)) - Resource not found with type/id:

```csharp
public class PpdsNotFoundException : PpdsException
{
    public string ResourceType { get; init; }
    public string ResourceId { get; init; }
}
```

### StructuredError

Serializable error container for consistent representation across UIs ([`StructuredError.cs:13-17`](../src/PPDS.Cli/Infrastructure/Errors/StructuredError.cs#L13-L17)):

```csharp
public record StructuredError(
    string Code,      // Hierarchical error code
    string Message,   // User-readable message
    string? Details,  // Stack trace (debug mode only)
    string? Target);  // Error context (file, entity)
```

### Usage Pattern

```csharp
// Service throwing
public async Task DeleteProfileAsync(string name)
{
    var profile = await _store.GetAsync(name);
    if (profile == null)
        throw new PpdsNotFoundException("Profile", name);

    await _store.DeleteAsync(name);
}

// CLI handling
try
{
    await service.DeleteProfileAsync(name);
    return ExitCodes.Success;
}
catch (PpdsException ex)
{
    ErrorOutput.WriteErrorLine(ex.UserMessage);
    return ExceptionMapper.ToExitCode(ex);
}
```

---

## Error Codes

Hierarchical codes organized by category ([`ErrorCodes.cs:12-202`](../src/PPDS.Cli/Infrastructure/Errors/ErrorCodes.cs#L12-L202)):

| Category | Codes | Purpose |
|----------|-------|---------|
| `Profile.*` | NotFound, NoActiveProfile, NameInUse, InvalidName | Profile management errors |
| `Auth.*` | ProfileNotFound, Expired, InvalidCredentials, InsufficientPermissions, CertificateError | Authentication failures |
| `Connection.*` | Failed, Throttled, Timeout, EnvironmentNotFound, DiscoveryFailed | Connection errors |
| `Validation.*` | RequiredField, InvalidValue, FileNotFound, SchemaInvalid | Input validation |
| `Operation.*` | NotFound, Duplicate, Dependency, PartialFailure, Cancelled, Internal | Operation failures |
| `Query.*` | ParseError, InvalidFetchXml, ExecutionFailed | Query errors |
| `External.*` | GitHubApiError, GitHubAuthError, ServiceUnavailable | External service errors |
| `Solution.*` | NotFound | Solution-related errors |
| `Plugin.*` | NotFound, NoContent, ManagedComponent, HasChildren | Plugin registration errors |

**Code Format:**

- Pattern: `Category.Subcategory`
- Example: `Auth.ProfileNotFound`, `Connection.Throttled`
- Enables both exact matching (`== ErrorCodes.Auth.Expired`) and category matching (`.StartsWith("Auth.")`)

---

## Exit Codes

Standard CLI exit codes for script integration ([`ExitCodes.cs:6-40`](../src/PPDS.Cli/Infrastructure/Errors/ExitCodes.cs#L6-L40)):

| Code | Name | Meaning |
|------|------|---------|
| 0 | Success | Operation completed successfully |
| 1 | PartialSuccess | Some records failed but operation completed |
| 2 | Failure | Operation failed - could not complete |
| 3 | InvalidArguments | Invalid command-line arguments |
| 4 | ConnectionError | Connection to Dataverse failed |
| 5 | AuthError | Authentication failed |
| 6 | NotFoundError | Resource not found (profile, environment, file) |
| 7 | MappingRequired | Auto-mapping incomplete, requires --generate-mapping |
| 8 | ValidationError | Validation error (schema mismatch, incomplete mapping) |
| 9 | Forbidden | Action not allowed (e.g., managed component) |
| 10 | PreconditionFailed | Operation blocked by current state (has children) |

**Exit Code Mapping** ([`ExceptionMapper.cs:40-78`](../src/PPDS.Cli/Infrastructure/Errors/ExceptionMapper.cs#L40-L78)):

```csharp
return ex switch
{
    PpdsAuthException => ExitCodes.AuthError,
    PpdsNotFoundException => ExitCodes.NotFoundError,
    PpdsValidationException => ExitCodes.InvalidArguments,
    PpdsThrottleException => ExitCodes.ConnectionError,
    PpdsException { ErrorCode: var code } when code.StartsWith("Auth.") => ExitCodes.AuthError,
    _ => ExitCodes.Failure
};
```

---

## Error Handling

### Error Types

| Error | Condition | Recovery |
|-------|-----------|----------|
| PpdsAuthException (RequiresReauth=true) | Token expired, invalid credentials | Trigger re-authentication flow |
| PpdsAuthException (RequiresReauth=false) | Insufficient permissions | Show error, no retry |
| PpdsThrottleException | Dataverse service protection (429) | Wait RetryAfter, then retry |
| PpdsValidationException | Invalid input data | Display field errors, let user correct |
| PpdsNotFoundException | Resource doesn't exist | Show error, no retry |

### Recovery Strategies

- **Authentication errors**: Check `RequiresReauthentication` flag; if true, prompt for login
- **Throttling**: Wait `RetryAfter` duration before retry; connection pool handles this automatically
- **Validation errors**: Display all field errors at once; don't bail on first error
- **Not found errors**: No automatic retry; resource genuinely doesn't exist

### Edge Cases

| Scenario | Expected Behavior |
|----------|-------------------|
| Nested exceptions | ExceptionMapper unwraps to find PPDS exception or maps outer |
| Unknown exception type | Maps to `Operation.Internal` with generic message |
| Exception with credentials | ConnectionStringRedactor removes sensitive values |
| Debug mode | StructuredError includes stack trace in Details |

---

## Design Decisions

### Why Structured Exceptions Over Error Codes?

**Context:** Needed consistent error handling across CLI, TUI, RPC, and VS Code interfaces. Each UI displays errors differently but needs the same underlying information.

**Decision:** Use exception hierarchy with embedded error codes rather than return-value error codes.

**Alternatives considered:**
- Result<T, E> pattern: Rejected - doesn't integrate with async/await naturally in C#
- Error codes only: Rejected - loses context, harder to include retry information
- Raw exceptions: Rejected - technical details leak to users

**Consequences:**
- Positive: Natural C# error flow, type-safe handling (catch specific exception types)
- Positive: Exceptions carry rich context (RetryAfter, ValidationErrors, etc.)
- Negative: Services must wrap lower-level exceptions

### Why Hierarchical Error Codes?

**Context:** VS Code extension and scripts need to handle errors programmatically. Some scenarios need specific handling (Profile.NotFound), others need category handling (any Auth.* error).

**Decision:** Use `Category.Subcategory` format for error codes (e.g., `Auth.ProfileNotFound`).

**Alternatives considered:**
- Flat string codes: Rejected - can't do category matching
- Numeric codes: Rejected - not self-documenting
- Enum per category: Rejected - harder to serialize consistently

**Consequences:**
- Positive: `code.StartsWith("Auth.")` enables category handling
- Positive: `== ErrorCodes.Auth.Expired` enables specific handling
- Positive: Self-documenting in logs and error messages

### Why Automatic Credential Redaction?

**Context:** Connection strings and exception messages may contain credentials. These should never appear in error output or logs.

**Decision:** `ConnectionStringRedactor` automatically removes sensitive values (ClientSecret, Password, Token, etc.) from all error messages.

**Implementation:** Regex pattern matches sensitive keys (`ClientSecret=...`) and replaces values with `***REDACTED***` ([`ConnectionStringRedactor.cs:20-67`](../src/PPDS.Dataverse/Security/ConnectionStringRedactor.cs#L20-L67)).

**Protected keys:**
```
ClientSecret, Password, Secret, Key, Pwd, Token, ApiKey,
AccessToken, RefreshToken, SharedAccessKey, AccountKey, Credential
```

**Consequences:**
- Positive: Credentials never leak to console, logs, or RPC responses
- Positive: Applied automatically via StructuredError.Create()
- Negative: Minor performance overhead from regex matching

### Why Separate Exception Types Per Domain?

**Context:** Different layers have different exception needs. PPDS.Auth handles credential acquisition, PPDS.Dataverse handles mid-operation auth failures, PPDS.Cli handles user-facing errors.

**Decision:** Each layer has its own exception types with domain-specific properties:
- `AuthenticationException` (PPDS.Auth): Credential acquisition failures, optional ErrorCode
- `DataverseAuthenticationException` (PPDS.Dataverse): Mid-operation auth failures, RequiresReauthentication flag
- `PpdsAuthException` (PPDS.Cli): User-facing auth errors with ErrorCode

**Consequences:**
- Positive: Each layer can include relevant context (ConnectionName, FailedOperation)
- Positive: ExceptionMapper consolidates them for UI layer
- Negative: Multiple exception types to understand

---

## Extension Points

### Adding a New Error Code

1. **Add constant to ErrorCodes**: Add to appropriate category class in [`ErrorCodes.cs`](../src/PPDS.Cli/Infrastructure/Errors/ErrorCodes.cs)
2. **Use in services**: Throw `PpdsException` with the new code
3. **Update exit code mapping** (if needed): Add case in [`ExceptionMapper.ToExitCode()`](../src/PPDS.Cli/Infrastructure/Errors/ExceptionMapper.cs#L40-L78)

```csharp
public static class ErrorCodes
{
    public static class Solution
    {
        public const string NotFound = "Solution.NotFound";
        public const string ImportFailed = "Solution.ImportFailed"; // New
    }
}
```

### Adding a New Exception Type

1. **Create subclass**: Inherit from `PpdsException` in [`PpdsException.cs`](../src/PPDS.Cli/Infrastructure/Errors/PpdsException.cs)
2. **Add domain-specific properties**: Include recovery information (RetryAfter, etc.)
3. **Update ExceptionMapper**: Add case in [`MapExceptionToCode()`](../src/PPDS.Cli/Infrastructure/Errors/ExceptionMapper.cs#L92-L160) and [`ToExitCode()`](../src/PPDS.Cli/Infrastructure/Errors/ExceptionMapper.cs#L40-L78)

```csharp
public class PpdsConcurrencyException : PpdsException
{
    public Guid RecordId { get; init; }
    public string EntityName { get; init; }

    public PpdsConcurrencyException(string entityName, Guid recordId)
        : base(ErrorCodes.Operation.Conflict,
               $"Record was modified by another user. Refresh and try again.")
    {
        EntityName = entityName;
        RecordId = recordId;
    }
}
```

---

## Testing

### Acceptance Criteria

- [x] All Application Services throw PpdsException or subclasses
- [x] Error codes follow Category.Subcategory format
- [x] UserMessage is free of technical jargon and stack traces
- [x] Credentials are redacted from error messages
- [x] CLI returns appropriate exit codes for all error types
- [x] ExceptionMapper handles all known exception types

### Test Coverage

Test suites in [`tests/PPDS.Cli.Tests/Infrastructure/Errors/`](../tests/PPDS.Cli.Tests/Infrastructure/Errors/):

| Test Class | Coverage |
|------------|----------|
| [ExceptionMapperTests.cs](../tests/PPDS.Cli.Tests/Infrastructure/Errors/ExceptionMapperTests.cs) | Exception-to-code mapping, exit code mapping |
| [ErrorCodesTests.cs](../tests/PPDS.Cli.Tests/Infrastructure/Errors/ErrorCodesTests.cs) | Code format validation, uniqueness |
| [StructuredErrorTests.cs](../tests/PPDS.Cli.Tests/Infrastructure/Errors/StructuredErrorTests.cs) | Error creation, redaction |
| [ExitCodesTests.cs](../tests/PPDS.Cli.Tests/Infrastructure/Errors/ExitCodesTests.cs) | Exit code value validation |

### Test Examples

```csharp
[Fact]
public void Map_AuthenticationException_ReturnsAuthCode()
{
    var ex = new AuthenticationException("Auth failed");
    var error = ExceptionMapper.Map(ex);

    Assert.Equal(ErrorCodes.Auth.InvalidCredentials, error.Code);
    Assert.Equal("Auth failed", error.Message);
}

[Fact]
public void ToExitCode_PpdsAuthException_ReturnsAuthError()
{
    var ex = new PpdsAuthException(ErrorCodes.Auth.Expired, "Session expired");
    var code = ExceptionMapper.ToExitCode(ex);

    Assert.Equal(ExitCodes.AuthError, code);
}
```

---

## Related Specs

- [architecture.md](./architecture.md) - Overall system architecture and layering
- [application-services.md](./application-services.md) - Service layer that throws PpdsException
- [cli.md](./cli.md) - CLI output handling and exit codes
- [authentication.md](./authentication.md) - Authentication failure scenarios

---

## Roadmap

- User message localization support
- Structured logging integration with error codes
- Error telemetry for common failure patterns
