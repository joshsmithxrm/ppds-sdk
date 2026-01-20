# PPDS.Auth: Token Management

## Overview

The Token Management subsystem handles MSAL token caching, Power Platform API token acquisition, and token lifecycle management. It provides persistent file-based caching for user-delegated tokens and supports multiple authentication flows including silent acquisition, interactive browser, and device code.

## Public API

### Classes

| Class | Purpose |
|-------|---------|
| `TokenCacheManager` | Manages token cache clearing operations |
| `MsalClientBuilder` | Creates and configures MSAL public client applications |
| `PowerPlatformTokenProvider` | Acquires tokens for Power Platform APIs |
| `MsalAccountHelper` | Finds cached accounts for silent authentication |
| `JwtClaimsParser` | Parses claims from JWT access tokens |

### Interfaces

| Interface | Purpose |
|-----------|---------|
| `IPowerPlatformTokenProvider` | Acquires tokens for Power Apps, Power Automate, and Flow APIs |

### DTOs/Models

| Type | Purpose |
|------|---------|
| `PowerPlatformToken` | Access token with expiration and identity info |
| `CachedTokenInfo` | Token cache status without triggering auth |

## Behaviors

### Token Cache Operations

| Operation | Description |
|-----------|-------------|
| `CreateAndRegisterCacheAsync` | Creates file-based MSAL cache with persistence verification |
| `UnregisterCache` | Cleans up cache registration |
| `ClearAllCachesAsync` | Deletes MSAL token cache file |
| `VerifyPersistence` | Tests cache write/read/clear operations |

### Power Platform Token Acquisition

| Method | Resource |
|--------|----------|
| `GetPowerAppsTokenAsync` | `https://api.powerapps.com` (varies by cloud) |
| `GetPowerAutomateTokenAsync` | `https://api.flow.microsoft.com` (varies by cloud) |
| `GetFlowApiTokenAsync` | `https://service.powerapps.com` (for Connections API) |
| `GetTokenForResourceAsync` | Any specified Power Platform resource |

### Authentication Flow Priority

1. **Silent acquisition**: Try cached tokens first (fastest)
2. **Interactive browser**: Open system browser for login (desktop)
3. **Device code**: Display URL + code for headless environments

### MSAL Client Configuration

| Setting | Value | Notes |
|---------|-------|-------|
| Client ID | `51f81489-12ee-4a9e-aaae-a2591f45987d` | Microsoft's public client ID |
| Authority | `{cloudInstance}/{tenantId}` | Cloud-specific authority |
| Redirect URI | `http://localhost` | For browser auth |
| Cache | Unprotected file | Linux compatibility |

### Lifecycle

- **Initialization**: MSAL client created lazily on first token request
- **Caching**: File-based cache registered with MSAL
- **Refresh**: MSAL handles token refresh transparently
- **Cleanup**: `Dispose` unregisters cache

## Edge Cases

| Scenario | Behavior | Notes |
|----------|----------|-------|
| Cache persistence fails | Warns user; continues without cache | MsalCachePersistenceException |
| Silent acquisition fails | Falls back to interactive | MsalUiRequiredException |
| No cached account | Skips directly to interactive | First-time auth |
| Browser unavailable | Falls back to device code | Headless environments |
| User cancels browser | Throws `OperationCanceledException` | Graceful cancel |
| Token expired | MSAL auto-refreshes | Transparent to caller |
| SPN calling Flow API | Limited functionality | User context required |

## Error Handling

| Exception | Condition | Recovery |
|-----------|-----------|----------|
| `AuthenticationException` | Token acquisition failed | Check credentials; retry |
| `MsalUiRequiredException` | Interactive auth needed | Trigger interactive flow |
| `MsalClientException` | MSAL error (e.g., canceled) | Handle specific error code |
| `MsalCachePersistenceException` | Cache cannot persist | Continue without cache |
| `OperationCanceledException` | User canceled | Handle gracefully |

## Configuration

### File Paths

| File | Location | Purpose |
|------|----------|---------|
| Token cache | `{DataDirectory}/msal_token_cache.bin` | MSAL token cache |

### Environment Variables

| Variable | Purpose |
|----------|---------|
| `PPDS_CONFIG_DIR` | Override data directory |

## Thread Safety

- **TokenCacheManager**: Thread-safe (static file operations)
- **MsalClientBuilder**: Thread-safe (creates new instances)
- **PowerPlatformTokenProvider**: Not thread-safe (single MSAL client per instance)
- **MSAL token cache**: Thread-safe (MSAL handles internally)

## SPN Limitations for Power Platform APIs

Service principals (client credentials) have limited Power Platform API access:

| API | SPN Support | Notes |
|-----|-------------|-------|
| Power Apps Admin API | Yes | Environment management |
| Power Automate Admin API | Yes | Flow management |
| Connections API | No | Requires user context |
| Some Flow operations | Limited | May need owner's context |

For full functionality, use interactive or device code authentication.

## Account Lookup for Silent Auth

The `MsalAccountHelper.FindAccountAsync` method searches cached accounts by priority:

1. **HomeAccountId**: Exact match on `{objectId}.{tenantId}` (most reliable)
2. **TenantId + Username**: Match on tenant and username
3. **Username only**: Match on username across all tenants (fallback)

## JWT Claims Parsing

The `JwtClaimsParser` extracts claims from access tokens:

| Claim | Description |
|-------|-------------|
| `oid` | Object ID (user or SPN) |
| `tid` | Tenant ID |
| `upn` | User Principal Name |
| `preferred_username` | Display username |
| `puid` | User PUID |
| `exp` | Token expiration |

## Dependencies

- **Internal**:
  - `PPDS.Auth.Profiles` - Profile configuration
  - `PPDS.Auth.Cloud` - Cloud-specific endpoints
- **External**:
  - `Microsoft.Identity.Client` (MSAL)
  - `Microsoft.Identity.Client.Extensions.Msal` (Token cache)
  - `Azure.Identity` (Client credentials)
  - `Azure.Core` (Token request context)

## Related

- [Profile Storage spec](./01-profile-storage.md) - Provides profile configuration
- [Credential Providers spec](./02-credential-providers.md) - Uses token management for auth
- [Cloud Support spec](./05-cloud-support.md) - Cloud-specific authority URLs

## Source Files

| File | Purpose |
|------|---------|
| `src/PPDS.Auth/Credentials/TokenCacheManager.cs` | Cache clearing |
| `src/PPDS.Auth/Credentials/MsalClientBuilder.cs` | MSAL client creation |
| `src/PPDS.Auth/Credentials/IPowerPlatformTokenProvider.cs` | Token provider interface |
| `src/PPDS.Auth/Credentials/PowerPlatformTokenProvider.cs` | Token provider implementation |
| `src/PPDS.Auth/Credentials/MsalAccountHelper.cs` | Account lookup for silent auth |
| `src/PPDS.Auth/Credentials/JwtClaimsParser.cs` | JWT claims extraction |
| `src/PPDS.Auth/AuthDebugLog.cs` | Debug logging for auth flows |
| `src/PPDS.Auth/AuthenticationOutput.cs` | User-facing auth messages |
