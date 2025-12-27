# PPDS Migration CLI v1 Enhancements Specification

**Status:** Partially Implemented
**Branch:** `feature/v2-alpha`
**Date:** 2025-12-27

---

## Overview

This specification covers CLI enhancements for the v1 release, building on the System.CommandLine 2.0.1 migration.

### Goals

1. **Better validation** - Fail fast with clear errors before execution
2. **Improved UX** - Tab completions, response files, directives
3. **Flexible authentication** - Support multiple auth methods for different scenarios
4. **Security** - Never accept secrets as CLI arguments

### Implementation Status

| Feature | Status | Notes |
|---------|--------|-------|
| Validators | Implemented | AcceptExistingOnly, AcceptLegalFileNamesOnly |
| Tab Completions | Not Started | Deferred |
| `--auth env` | Implemented | Environment variable auth |
| `--auth interactive` | Implemented | Device code flow (default) |
| `--auth managed` | Implemented | Azure Managed Identity |
| `--auth config` | **Abandoned** | See [Decision: Config-Based Auth](#decision-config-based-auth-abandoned) |
| `--env` option | **Abandoned** | See [Decision: Config-Based Auth](#decision-config-based-auth-abandoned) |
| `--secrets-id` option | **Abandoned** | See [Decision: Config-Based Auth](#decision-config-based-auth-abandoned) |

---

## Decision: Config-Based Auth (Abandoned)

**Date:** 2025-12-27

The original spec included `--auth config`, `--env`, and `--secrets-id` options for configuration-file-based authentication. These have been **abandoned** in favor of the simpler URL-based approach.

### Original Design

```bash
# Would have required appsettings.json with environment definitions
ppds-migrate export --env Dev --secrets-id ppds-dataverse-demo --schema schema.xml
```

### Why Abandoned

1. **Primary consumer changed** - Demo app now uses PPDS.Migration library directly instead of shelling out to CLI. The `--secrets-id` option was designed for cross-process User Secrets sharing, which is no longer needed.

2. **Configuration file discovery is ambiguous** - Where should CLI look for appsettings.json? CWD changes behavior unexpectedly. Explicit `--config` path is as verbose as `--url`.

3. **Current auth modes cover all use cases**:
   - Interactive (default): Ad-hoc developer usage
   - Environment variables: CI/CD pipelines
   - Managed identity: Azure-hosted workloads

4. **Simpler is better** - `--url` is explicit and unambiguous. No configuration file discovery complexity.

### Current Design

```bash
# Explicit URL - no config file needed
ppds-migrate export --url https://contoso.crm.dynamics.com --schema schema.xml --output data.zip

# CI/CD with environment variables
export DATAVERSE__URL="https://contoso.crm.dynamics.com"
export DATAVERSE__CLIENTID="..."
export DATAVERSE__CLIENTSECRET="..."
ppds-migrate export --auth env --schema schema.xml --output data.zip
```

### Library vs CLI Configuration

Note: The **library** (PPDS.Dataverse, PPDS.Migration) fully supports multi-environment configuration via appsettings.json. This is appropriate for library consumers who integrate via DI.

The **CLI** uses explicit `--url` because:
- CLI is standalone, not integrated into a host application
- CLI users expect explicit, predictable behavior
- No ambiguity about which configuration file is used

---

## 1. Validators

### 1.1 File Validators

Use built-in `AcceptExistingOnly()` for input files and `AcceptLegalFileNamesOnly()` for output files.

| Option | Validator | Commands |
|--------|-----------|----------|
| `--schema` | `AcceptExistingOnly()` | export, migrate, analyze |
| `--data` | `AcceptExistingOnly()` | import |
| `--config` | `AcceptExistingOnly()` | all |
| `--user-mapping` | `AcceptExistingOnly()` | import |
| `--output` | `AcceptLegalFileNamesOnly()` | export, schema generate |

**Before (handler validation):**
```csharp
command.SetAction(async (parseResult, token) =>
{
    var schema = parseResult.GetValue(schemaOption)!;
    if (!schema.Exists)
    {
        ConsoleOutput.WriteError($"Schema file not found: {schema.FullName}", json);
        return ExitCodes.InvalidArguments;
    }
    // ...
});
```

**After (declarative validation):**
```csharp
var schemaOption = new Option<FileInfo>("--schema", "-s")
{
    Description = "Path to schema.xml file",
    Required = true
}.AcceptExistingOnly();

// Handler no longer needs existence check - validation happens before handler runs
```

### 1.2 Numeric Validators

Custom validators for options with constraints.

| Option | Constraint | Commands |
|--------|------------|----------|
| `--parallel` | Must be ≥ 1 | export |
| `--page-size` | Must be ≥ 1, ≤ 5000 | export |

**Implementation:**
```csharp
var parallelOption = new Option<int>("--parallel")
{
    Description = "Degree of parallelism for concurrent entity exports",
    DefaultValueFactory = _ => Environment.ProcessorCount * 2
};
parallelOption.Validators.Add(result =>
{
    var value = result.GetValue(parallelOption);
    if (value < 1)
        result.AddError("--parallel must be at least 1");
});
```

---

## 2. Tab Completions

> **Status:** Deferred. Tab completions for entity names and auth modes may be added in a future release.

~~### 2.1 Environment Name Completion~~ (Abandoned - see [Decision: Config-Based Auth](#decision-config-based-auth-abandoned))

---

## 3. Authentication Architecture

### 3.1 Auth Modes

| Mode | Flag | Description | Use Case | Status |
|------|------|-------------|----------|--------|
| `interactive` | `--auth interactive` (default) | Device code flow (browser) | Development, ad-hoc | **Implemented** |
| `env` | `--auth env` | Environment variables only | CI/CD, containers | **Implemented** |
| `managed` | `--auth managed` | Azure Managed Identity | Azure-hosted production | **Implemented** |
| ~~`config`~~ | ~~`--auth config`~~ | ~~appsettings.json + User Secrets~~ | ~~Development~~ | **Abandoned** |

### 3.2 Authentication Resolution

The CLI requires explicit `--url` for the target environment. When `--auth` is not specified, interactive mode is used (device code flow).

### 3.3 Environment Variable Schema

| Variable | Description | Required |
|----------|-------------|----------|
| `DATAVERSE__URL` | Environment URL (e.g., https://org.crm.dynamics.com) | Yes |
| `DATAVERSE__CLIENTID` | Azure AD Application (client) ID | For ClientCredentials |
| `DATAVERSE__CLIENTSECRET` | Azure AD Client Secret | For ClientCredentials |
| `DATAVERSE__TENANTID` | Azure AD Tenant ID | Optional (auto-discovered) |

**Alternative prefix:** `PPDS__DATAVERSE__*` (for namespacing in complex environments)

### 3.4 Interactive Auth (Device Code Flow)

```bash
ppds-migrate export --auth interactive --env Dev --schema schema.xml --output data.zip
```

**Flow:**
1. CLI displays: "To sign in, use a web browser to open https://microsoft.com/devicelogin and enter the code XXXXXXXX"
2. User opens browser, enters code, authenticates
3. CLI receives token, proceeds with operation
4. Token cached for future use (until expiry)

**Implementation:**
```csharp
// Using MSAL or Dataverse Client's built-in support
var client = new ServiceClient(
    new Uri(url),
    authType: AuthenticationType.OAuth,
    promptBehavior: PromptBehavior.Auto);
```

### 3.5 Managed Identity

```bash
ppds-migrate export --auth managed --env Prod --schema schema.xml --output data.zip
```

**Implementation:**
```csharp
// Using Azure.Identity
var credential = new DefaultAzureCredential(new DefaultAzureCredentialOptions
{
    ExcludeInteractiveBrowserCredential = true,
    ExcludeVisualStudioCredential = true,
    // Only use managed identity
    ExcludeManagedIdentityCredential = false
});

var client = new ServiceClient(
    instanceUrl: new Uri(url),
    tokenProviderFunction: async (url) =>
    {
        var token = await credential.GetTokenAsync(
            new TokenRequestContext(new[] { $"{url}/.default" }));
        return token.Token;
    });
```

**Required package:** `Azure.Identity`

### 3.6 Security Rules

| Rule | Enforcement |
|------|-------------|
| Never accept `--client-secret` as CLI argument | Not implemented as option |
| Never accept `--connection-string` as CLI argument | Not implemented as option |
| Clear sensitive env vars after use (PowerShell) | Documented best practice |
| Token caching for interactive auth | Use MSAL cache |

---

## 4. CLI Option Changes

### 4.1 Global Options (Implemented)

```csharp
public static readonly Option<string?> UrlOption = new("--url")
{
    Description = "Dataverse environment URL (e.g., https://org.crm.dynamics.com)",
    Recursive = true
};

public static readonly Option<AuthMode> AuthOption = new("--auth")
{
    Description = "Authentication mode: interactive (default), env, managed",
    DefaultValueFactory = _ => AuthMode.Interactive,
    Recursive = true
};

public enum AuthMode
{
    Interactive, // Device code flow (default)
    Env,         // Environment variables
    Managed      // Azure Managed Identity
}
```

### 4.2 Current Help Output

```
Description:
  PPDS Migration CLI - High-performance Dataverse data migration tool

Usage:
  ppds-migrate [command] [options]

Options:
  --url <url>                       Dataverse environment URL (e.g., https://org.crm.dynamics.com)
  --auth <Env|Interactive|Managed>  Authentication mode: interactive (default), env, managed [default: Interactive]
  -?, -h, --help                    Show help and usage information
  --version                         Show version information

Commands:
  export   Export data from Dataverse to a ZIP file
  import   Import data from a ZIP file into Dataverse
  analyze  Analyze schema and display dependency graph
  migrate  Migrate data from source to target Dataverse environment
  schema   Generate and manage migration schemas
  users    User mapping commands for cross-environment migration
```

---

## 5. Documentation Updates

### 5.1 Response Files

Document in README:
```markdown
## Response Files

Store frequently-used options in a file:

```
# export-dev.rsp
export
--env
Dev
--schema
schema.xml
--output
data.zip
```

Run with:
```bash
ppds-migrate @export-dev.rsp
```
```

### 5.2 Directives

Document in README:
```markdown
## Debugging

### Parse Diagram
See how your command is parsed:
```bash
ppds-migrate [diagram] export --env Dev --schema schema.xml
```

### Suggest Commands
Find commands by partial name:
```bash
ppds-migrate [suggest] exp
# Output: export
```
```

### 5.3 Authentication Matrix

Document in README:
```markdown
## Authentication

| Scenario | Recommended | Command |
|----------|-------------|---------|
| Development / ad-hoc | Interactive | `--auth interactive` (default) |
| CI/CD pipeline | Environment vars | `--auth env` with `DATAVERSE__*` vars |
| Azure hosted | Managed Identity | `--auth managed` |
```

---

## 6. Dependencies

### New Packages

| Package | Version | Purpose |
|---------|---------|---------|
| `Azure.Identity` | Latest stable | Managed Identity, DefaultAzureCredential |

### Existing Packages (no changes)

- `Microsoft.PowerPlatform.Dataverse.Client` - already supports TokenCredential
- `System.CommandLine` - already updated to 2.0.1

---

## 7. Implementation Order

| Step | Feature | Status |
|------|---------|--------|
| 1 | Validators (AcceptExistingOnly, numeric) | **Done** |
| 2 | ~~Tab Completions~~ | Deferred |
| 3 | Environment Variable Auth (DATAVERSE__*) | **Done** |
| 4 | Auth Infrastructure (--auth option, AuthMode) | **Done** |
| 5 | Interactive Auth (device code flow) | **Done** |
| 6 | Managed Identity (Azure.Identity) | **Done** |
| 7 | Documentation (README updates) | **Done** |
| - | ~~Config-based auth (--env, --secrets-id)~~ | **Abandoned** |

---

## 8. Testing Strategy

### Unit Tests

- Validator behavior (file exists, numeric ranges)
- Auth mode resolution
- Environment variable parsing

### Integration Tests

- Interactive auth: Manual testing with real Azure AD
- Managed Identity: Test in Azure VM or use emulator
- Environment variable auth: CI/CD pipeline testing

### Manual Testing Checklist

- [x] `ppds-migrate --help` shows --auth option
- [x] `ppds-migrate export --auth interactive` prompts device code
- [x] `ppds-migrate export --auth env` uses environment variables
- [ ] `ppds-migrate export --auth managed` works in Azure VM
- [x] Invalid file paths show clear error before handler runs
- [ ] `@response.rsp` file works
- [ ] `[diagram]` directive works

---

## 9. Breaking Changes

None. All changes are additive:
- New `--auth` option (default maintains current behavior)
- Validators provide earlier/clearer errors but same outcome
- Completions are UX enhancement only
