# PPDS.Auth: Profile Storage

## Overview

The Profile Storage subsystem provides persistent storage for authentication profiles, managing JSON-based profile collections with index-based tracking, schema versioning, and in-memory caching. Profiles store authentication configuration (method, cloud, tenant, environment binding) while secrets are delegated to the secure credential store.

## Public API

### Classes

| Class | Purpose |
|-------|---------|
| `ProfileStore` | Manages persistent storage of profiles to disk |
| `ProfileCollection` | In-memory collection with active profile tracking |
| `AuthProfile` | Single authentication profile configuration |
| `EnvironmentInfo` | Dataverse environment binding information |
| `ProfilePaths` | Platform-specific path resolution |

### Enums

| Enum | Purpose |
|------|---------|
| `AuthMethod` | Authentication methods (interactive, service principal, federated, etc.) |
| `CloudEnvironment` | Cloud regions (Public, GCC, GCCHigh, DoD, China, etc.) |

## Behaviors

### Profile Store Operations

| Method | Description |
|--------|-------------|
| `LoadAsync` / `Load` | Load collection from disk (returns cached if available) |
| `SaveAsync` / `Save` | Save collection to disk (updates cache) |
| `UpdateProfileAsync` | Load, apply update action, save atomically |
| `Delete` | Delete storage file and clear cache |
| `ClearCache` | Force reload on next access |

### Profile Collection Operations

| Method | Description |
|--------|-------------|
| `Add` | Add profile with auto-assigned index |
| `GetByIndex` | Retrieve by 1-based index |
| `GetByName` | Retrieve by name (case-insensitive) |
| `GetByNameOrIndex` | Flexible lookup (name, "N", "[N]", "[N] Name") |
| `RemoveByIndex` / `RemoveByName` | Remove profile |
| `SetActiveByIndex` / `SetActiveByName` | Set active profile |
| `IsNameInUse` | Check for name conflicts |

### Schema Versioning

| Version | Format | Active Tracking |
|---------|--------|-----------------|
| v1 (legacy) | Profiles as dictionary | `activeIndex` (int) |
| v2 (current) | Profiles as array | `activeProfileIndex` + `activeProfileName` |

**Migration**: v1 files are automatically detected and deleted with user warning.

### Lifecycle

- **Initialization**: Store created with file path (default: `ProfilePaths.ProfilesFile`)
- **Loading**: File read, parsed; cached in memory with locking
- **Saving**: Collection serialized to JSON; cache updated
- **Cleanup**: `Dispose` releases lock semaphore

## Edge Cases

| Scenario | Behavior | Notes |
|----------|----------|-------|
| File not found | Returns empty collection | Normal first-run |
| v1 schema detected | Deletes file, returns empty | Breaking change logged |
| Invalid JSON | Treated as v1 (deleted) | Defensive handling |
| Duplicate profile name | Rejected by `IsNameInUse` check | Case-insensitive |
| Remove active profile | Auto-selects lowest-indexed remaining | Or null if none |
| Profile without name | Valid; referenced by index | `[N]` format |
| Profile with name | Displayed as `[N] Name` | Both index and name work |

## Error Handling

| Exception | Condition | Recovery |
|-----------|-----------|----------|
| `InvalidOperationException` | Duplicate index, profile not found for set-active | Check collection state |
| `ArgumentNullException` | Null collection or update action | Validation error |
| `IOException` | File access error | Check permissions |

## Storage Format

### File Location

| Platform | Default Path |
|----------|--------------|
| Windows | `%LOCALAPPDATA%\PPDS\profiles.json` |
| macOS/Linux | `~/.ppds/profiles.json` |

Override via `PPDS_CONFIG_DIR` environment variable.

### JSON Schema (v2)

```json
{
  "version": 2,
  "activeProfileIndex": 1,
  "activeProfile": "dev",
  "profiles": [
    {
      "index": 1,
      "name": "dev",
      "authMethod": "interactiveBrowser",
      "cloud": "public",
      "tenantId": "...",
      "username": "user@example.com",
      "objectId": "...",
      "environment": {
        "url": "https://org.crm.dynamics.com/",
        "displayName": "Dev Org"
      },
      "createdAt": "2024-01-01T00:00:00Z",
      "lastUsedAt": "2024-01-02T00:00:00Z",
      "homeAccountId": "objectId.tenantId",
      "authority": "https://login.microsoftonline.com/tenantId"
    }
  ]
}
```

## Configuration

| Setting | Type | Description |
|---------|------|-------------|
| `PPDS_CONFIG_DIR` | env var | Override data directory |

## Thread Safety

- **ProfileStore**: Thread-safe via `SemaphoreSlim` for all file operations
- **ProfileCollection**: Not thread-safe; obtain from store and save after modifications
- **AuthProfile**: Not thread-safe; clone for concurrent use

## AuthProfile Properties

### Identity

| Property | Description |
|----------|-------------|
| `Index` | 1-based index (assigned on creation) |
| `Name` | Optional name (max 30 chars) |
| `AuthMethod` | Authentication method enum |
| `Cloud` | Cloud environment enum |
| `TenantId` | Azure AD tenant ID |

### User Authentication

| Property | Description |
|----------|-------------|
| `Username` | UPN from token (populated after auth) |
| `ObjectId` | Entra ID Object ID |
| `Puid` | User PUID from JWT |
| `HomeAccountId` | MSAL account identifier |
| `Authority` | Full authority URL |

### Application Authentication

| Property | Description |
|----------|-------------|
| `ApplicationId` | App registration client ID |
| `CertificatePath` | Path to PFX file |
| `CertificateThumbprint` | Certificate thumbprint |
| `CertificateStoreName` | Store name (default: My) |
| `CertificateStoreLocation` | Store location (default: CurrentUser) |

### Environment Binding

| Property | Description |
|----------|-------------|
| `Environment` | Bound Dataverse environment (null = universal) |

### Metadata

| Property | Description |
|----------|-------------|
| `CreatedAt` | Profile creation timestamp |
| `LastUsedAt` | Last use timestamp |

## AuthMethod Values

| Method | Description | Required Fields |
|--------|-------------|-----------------|
| `InteractiveBrowser` | Browser popup flow | (none) |
| `DeviceCode` | Code+URL flow for headless | (none) |
| `ClientSecret` | App + secret | ApplicationId, TenantId |
| `CertificateFile` | App + PFX file | ApplicationId, CertificatePath, TenantId |
| `CertificateStore` | App + Windows cert store | ApplicationId, CertificateThumbprint, TenantId |
| `ManagedIdentity` | Azure managed identity | (ApplicationId optional for user-assigned) |
| `GitHubFederated` | GitHub Actions OIDC | ApplicationId, TenantId |
| `AzureDevOpsFederated` | Azure DevOps OIDC | ApplicationId, TenantId |
| `UsernamePassword` | ROPC flow | Username |

**Note**: Secrets (passwords, client secrets) are stored in secure credential store, not in profile.

## Related

- [Credential Providers spec](./02-credential-providers.md) - Uses profiles for auth config
- [Token Management spec](./03-token-management.md) - Caches tokens per profile
- [Secure Credential Store](./02-credential-providers.md) - Stores secrets separately

## Source Files

| File | Purpose |
|------|---------|
| `src/PPDS.Auth/Profiles/ProfileStore.cs` | Persistent storage |
| `src/PPDS.Auth/Profiles/ProfileCollection.cs` | In-memory collection |
| `src/PPDS.Auth/Profiles/AuthProfile.cs` | Profile data model |
| `src/PPDS.Auth/Profiles/EnvironmentInfo.cs` | Environment binding |
| `src/PPDS.Auth/Profiles/ProfilePaths.cs` | Path resolution |
| `src/PPDS.Auth/Profiles/AuthMethod.cs` | Auth method enum |
| `src/PPDS.Auth/Cloud/CloudEnvironment.cs` | Cloud environment enum |
