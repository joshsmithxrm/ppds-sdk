# ADR-0015: Application Service Layer for CLI/TUI/Daemon

**Status:** Accepted
**Date:** 2026-01-05
**Authors:** Josh, Claude

## Context

The PPDS CLI has three presentation layers that duplicate business logic:

1. **CLI Commands** (`SqlCommand.cs`) - System.CommandLine handlers with embedded parse/execute logic
2. **TUI Wizards** (`SqlQueryWizard.cs`) - Spectre.Console interactive mode with duplicated logic
3. **Daemon RPC** (`RpcMethodHandler.cs`) - JSON-RPC handlers with their own implementations

This duplication causes:
- **Bugs from inconsistency**: The TUI had `includeCount: true` causing errors with `TOP` queries that the CLI didn't have
- **Maintenance burden**: Changes must be made in multiple places
- **Testing difficulty**: Business logic is coupled to UI concerns

Example of current duplication (SQL query execution):

```
SqlCommand.cs:131-141        → Parse SQL → Transpile → Execute
SqlQueryWizard.cs:71-101     → Parse SQL → Transpile → Execute (duplicated)
RpcMethodHandler.cs:590-652  → Parse SQL → Transpile → Execute (duplicated)
```

## Decision

Extract **Application Services** that encapsulate business logic. CLI, TUI, and Daemon become thin presentation adapters that consume the same services.

### Architecture

```
┌─────────────┐  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐
│   CLI Cmd   │  │     TUI     │  │   Daemon    │  │  VS Code    │
│(SqlCommand) │  │(SqlWizard)  │  │(RpcHandler) │  │ (via RPC)   │
└──────┬──────┘  └──────┬──────┘  └──────┬──────┘  └──────┬──────┘
       │                │                │                │
       └────────────────┴────────────────┴────────────────┘
                               │
                    ┌──────────▼──────────┐
                    │  Application Services│
                    │  (ISqlQueryService)  │
                    └──────────┬──────────┘
                               │
              ┌────────────────┼────────────────┐
              │                │                │
       ┌──────▼──────┐  ┌──────▼──────┐  ┌──────▼──────┐
       │  SqlParser  │  │ Transpiler  │  │IQueryExecutor│
       └─────────────┘  └─────────────┘  └─────────────┘
```

### Key Principles

1. **Services return domain objects** - Presentation layers format output for their medium
2. **TUI calls services directly** - Same process, no IPC overhead needed
3. **Daemon wraps services for external consumers** - VS Code extension uses RPC
4. **Services live in `PPDS.Cli/Services/`** - Not a new NuGet package

### Service Interface Pattern

```csharp
public interface ISqlQueryService
{
    string TranspileSql(string sql, int? topOverride = null);
    Task<SqlQueryResult> ExecuteAsync(SqlQueryRequest request, CancellationToken ct);
}

public sealed record SqlQueryRequest
{
    public required string Sql { get; init; }
    public int? TopOverride { get; init; }
    public int? PageNumber { get; init; }
    public string? PagingCookie { get; init; }
    public bool IncludeCount { get; init; }
}

public sealed class SqlQueryResult
{
    public required string OriginalSql { get; init; }
    public required string TranspiledFetchXml { get; init; }
    public required QueryResult Result { get; init; }
}
```

### Output Formatting

Output formatting stays in the presentation layer because each consumer has different requirements:

| Consumer | Output Format |
|----------|---------------|
| CLI | Text tables, CSV, JSON (`IOutputWriter`) |
| TUI | Spectre.Console rich tables |
| Daemon | JSON-RPC response DTOs |

Services return `SqlQueryResult` (domain object). Presentation layers transform to their output format.

### Dependency Injection

Services are registered via `AddCliApplicationServices()` extension method, called from `ProfileServiceFactory.CreateProviderFromSources()`:

```csharp
public static class ServiceRegistration
{
    public static IServiceCollection AddCliApplicationServices(this IServiceCollection services)
    {
        services.AddTransient<ISqlQueryService, SqlQueryService>();
        // Future: IAuthProfileService, IEnvironmentService, etc.
        return services;
    }
}
```

## Consequences

### Positive

- **Single source of truth** - Business logic lives in one place
- **Testable** - Services can be unit tested without UI dependencies
- **Consistent behavior** - CLI, TUI, and Daemon produce identical results
- **Extensible** - Pattern extends to auth, environment, plugins, data operations

### Negative

- **More files** - Additional interface/implementation/DTO files
- **Indirection** - Commands now delegate to services instead of inline logic
- **Migration effort** - Existing code must be refactored

### Neutral

- **No new package** - Services stay in PPDS.Cli to avoid proliferation
- **Daemon unchanged for now** - RpcMethodHandler migration is future work

## Implementation Order

1. **SQL Query Service** (this PR) - Validate pattern with SqlCommand + SqlQueryWizard
2. **Auth Profile Service** (future) - List, select, who operations
3. **Environment Service** (future) - List, select, discovery operations
4. **Plugin Service** (future) - Deploy, extract, list operations
5. **Data Service** (future) - Export, import, copy operations

## References

- ADR-0007: Unified CLI and Auth Profiles
- ADR-0008: CLI Output Architecture
- ADR-0009: CLI Command Taxonomy
