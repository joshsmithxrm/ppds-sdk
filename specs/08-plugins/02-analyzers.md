# PPDS.Plugins: Analyzers

## Overview

PPDS.Analyzers is a Roslyn-based analyzer package that enforces architectural patterns and correctness rules at compile-time. It integrates with Visual Studio and the build process to provide real-time feedback on code quality. The package currently implements 3 of 13 planned diagnostic rules.

## Public API

### Diagnostic Categories

| Category | Purpose |
|----------|---------|
| `PPDS.Architecture` | Architectural enforcement |
| `PPDS.Performance` | Performance optimization |
| `PPDS.Correctness` | Safety and correctness |
| `PPDS.Style` | Code style recommendations |

### Diagnostic IDs

| ID | Category | Status | Purpose |
|----|----------|--------|---------|
| PPDS001 | Architecture | Planned | NoDirectFileIoInUi |
| PPDS002 | Architecture | Planned | NoConsoleInServices |
| PPDS003 | Architecture | Planned | NoUiFrameworkInServices |
| PPDS004 | Architecture | Planned | UseStructuredExceptions |
| PPDS005 | Architecture | Planned | NoSdkInPresentation |
| PPDS006 | Style | **Implemented** | UseEarlyBoundEntities |
| PPDS007 | Architecture | Planned | PoolClientInParallel |
| PPDS008 | Performance | Planned | UseBulkOperations |
| PPDS009 | Performance | Planned | UseAggregateForCount |
| PPDS010 | Performance | Planned | ValidateTopCount |
| PPDS011 | Correctness | Planned | PropagateCancellation |
| PPDS012 | Correctness | **Implemented** | NoSyncOverAsync |
| PPDS013 | Correctness | **Implemented** | NoFireAndForgetInCtor |

## Implemented Analyzers

### PPDS006: UseEarlyBoundEntities

Enforces use of early-bound entity constants instead of string literals.

**Severity:** Warning

**Detection:**
```csharp
// Flagged - string literal in QueryExpression
var query = new QueryExpression("account");

// Correct - early-bound constant
var query = new QueryExpression(Account.EntityLogicalName);
```

**Message:**
```
Use '{0}.EntityLogicalName' instead of string literal "{1}"
```

**Entity Mappings:**

| String Literal | Suggested Type |
|----------------|----------------|
| `pluginassembly` | `PluginAssembly` |
| `plugintype` | `PluginType` |
| `sdkmessage` | `SdkMessage` |
| `sdkmessageprocessingstep` | `SdkMessageProcessingStep` |
| `sdkmessageprocessingstepimage` | `SdkMessageProcessingStepImage` |
| `solution` | `Solution` |
| `solutioncomponent` | `SolutionComponent` |
| `asyncoperation` | `AsyncOperation` |
| `systemuser` | `SystemUser` |
| `publisher` | `Publisher` |
| `workflow` | `Workflow` |
| `plugintracelog` | `PluginTraceLog` |
| (unknown) | PascalCase conversion |

---

### PPDS012: NoSyncOverAsync

Prevents sync-over-async patterns that can cause deadlocks.

**Severity:** Warning

**Detection Patterns:**

| Pattern | Example | Risk |
|---------|---------|------|
| `.Result` | `task.Result` | Deadlock |
| `.Wait()` | `task.Wait()` | Deadlock |
| `.GetAwaiter().GetResult()` | `task.GetAwaiter().GetResult()` | Deadlock |

**Message:**
```
'{0}' can cause deadlocks; use 'await' instead
```

**Correct Pattern:**
```csharp
// Instead of
var result = DoAsync().Result;  // VIOLATION

// Use
var result = await DoAsync();   // Correct
```

**Types Detected:**
- `Task` / `Task<T>`
- `ValueTask` / `ValueTask<T>`

---

### PPDS013: NoFireAndForgetInCtor

Detects fire-and-forget async calls in constructors.

**Severity:** Warning

**Detection:**
```csharp
public class MyWindow : Window
{
    public MyWindow()
    {
        // VIOLATION - fire-and-forget
        LoadDataAsync();
    }
}
```

**Message:**
```
Async method '{0}' called in constructor without await; use Loaded event for async initialization
```

**Safe Patterns (Not Flagged):**
- Calls with `await` expression
- `Task.FromResult()`, `Task.FromException()`, `Task.FromCanceled()`
- Calls with `.ContinueWith()` error handling

**Correct Patterns:**
```csharp
public MyWindow()
{
    // Pattern 1: Use Loaded event
    Loaded += (s, e) => _ = LoadDataAsync();

    // Pattern 2: Explicit error handling
    _ = LoadDataAsync().ContinueWith(t => {
        if (t.IsFaulted) Logger.Error(t.Exception);
    });
}
```

## Behaviors

### Analyzer Execution

Analyzers run during:
- IDE code analysis (real-time)
- Build process (compile-time)
- CI/CD pipeline builds

### Error Reporting

All implemented analyzers use **Warning** severity by default. They do not block compilation but are reported in:
- Error List window (VS)
- Build output
- CI logs

## Edge Cases

| Scenario | Behavior | Notes |
|----------|----------|-------|
| Unknown entity name | PascalCase conversion | PPDS006 suggests capitalized name |
| Parenthesized expressions | Traversed correctly | PPDS012 handles `((task)).Result` |
| Nested constructors | Analyzed | PPDS013 checks all invocations |
| Static constructors | Analyzed | Same rules apply |

## Error Handling

N/A - Analyzers do not throw exceptions. They report diagnostics to the Roslyn framework.

## Dependencies

- **Internal:** None
- **External:**
  - `Microsoft.CodeAnalysis.CSharp` 5.0.0
  - `Microsoft.CodeAnalysis.CSharp.Workspaces` 5.0.0

## Configuration

### Project Configuration

```xml
<PropertyGroup>
  <TargetFramework>netstandard2.0</TargetFramework>
  <IsRoslynComponent>true</IsRoslynComponent>
  <EnforceExtendedAnalyzerRules>true</EnforceExtendedAnalyzerRules>
</PropertyGroup>
```

### Suppression Methods

**Inline Suppression:**
```csharp
#pragma warning disable PPDS006
var query = new QueryExpression("customentity"); // No early-bound
#pragma warning restore PPDS006
```

**EditorConfig:**
```ini
[*.cs]
# Disable analyzer
dotnet_diagnostic.PPDS006.severity = none

# Change to suggestion
dotnet_diagnostic.PPDS012.severity = suggestion

# Promote to error
dotnet_diagnostic.PPDS013.severity = error
```

**Global Suppression (assembly-level):**
```csharp
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage(
    "PPDS.Style", "PPDS006:UseEarlyBoundEntities",
    Justification = "Custom entity")]
```

### Severity Levels

| Level | Build Behavior | Default Usage |
|-------|----------------|---------------|
| `error` | Fails build | Not used by default |
| `warning` | Reports warning | All current analyzers |
| `suggestion` | IDE hint only | Available via config |
| `silent` | Hidden | Available via config |
| `none` | Disabled | Available via config |

## Thread Safety

Analyzers are stateless and thread-safe by design. Each analysis context receives its own instance.

## Code Fix Providers

**Status:** None implemented

The current analyzers do not include automatic code fixes. Users must manually refactor to resolve violations.

## Known Safe Patterns

| Location | Pattern | Suppression |
|----------|---------|-------------|
| Terminal.Gui Application.Run() | Sync required by framework | PPDS012 |
| DI factory delegates | Cannot be async | PPDS012 |
| ContinueWith error handling | Errors explicitly handled | PPDS013 |

## Related

- [PPDS.Plugins: Attributes](01-attributes.md) - Plugin registration attributes
- ADR-0015: Application Service Layer
- ADR-0026: Structured Error Model

## Source Files

| File | Purpose |
|------|---------|
| `src/PPDS.Analyzers/PPDS.Analyzers.csproj` | Project configuration |
| `src/PPDS.Analyzers/DiagnosticIds.cs` | ID constants |
| `src/PPDS.Analyzers/Rules/UseEarlyBoundEntitiesAnalyzer.cs` | PPDS006 |
| `src/PPDS.Analyzers/Rules/NoSyncOverAsyncAnalyzer.cs` | PPDS012 |
| `src/PPDS.Analyzers/Rules/NoFireAndForgetInCtorAnalyzer.cs` | PPDS013 |
