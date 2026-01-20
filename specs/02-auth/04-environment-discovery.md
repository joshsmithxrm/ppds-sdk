# PPDS.Auth: Environment Discovery

## Overview

The Environment Discovery subsystem uses Microsoft's Global Discovery Service to find all Dataverse environments accessible to an authenticated user. It supports silent and interactive authentication flows, captures account identifiers for token caching, and provides flexible environment resolution by name, URL, or ID.

## Public API

### Interfaces

| Interface | Purpose |
|-----------|---------|
| `IGlobalDiscoveryService` | Discovers environments via Global Discovery API |

### Classes

| Class | Purpose |
|-------|---------|
| `GlobalDiscoveryService` | Implementation using MSAL and ServiceClient.DiscoverOnlineOrganizationsAsync |
| `EnvironmentResolver` | Resolves environments by identifier from a collection |
| `EnvironmentResolutionService` | High-level service combining discovery and resolution |

### DTOs/Models

| Type | Purpose |
|------|---------|
| `DiscoveredEnvironment` | Environment metadata from discovery |

### Exceptions

| Exception | Purpose |
|-----------|---------|
| `AmbiguousMatchException` | Multiple environments match the given identifier |

## Behaviors

### Environment Discovery

1. **Initialize MSAL**: Create public client with multi-tenant ("organizations") authority
2. **Token acquisition**: Try silent first, fall back to interactive
3. **Call Discovery API**: Use `ServiceClient.DiscoverOnlineOrganizationsAsync`
4. **Map results**: Transform SDK organizations to `DiscoveredEnvironment`
5. **Capture HomeAccountId**: Store for subsequent silent auth

### Supported Auth Methods

| Auth Method | Support | Notes |
|-------------|---------|-------|
| `InteractiveBrowser` | Yes | Opens system browser |
| `DeviceCode` | Yes | Displays URL + code |
| `ClientSecret` | No | SPNs cannot use Global Discovery |
| `CertificateFile/Store` | No | SPNs cannot use Global Discovery |
| `ManagedIdentity` | No | Cannot use Global Discovery |
| `GitHubFederated` | No | Cannot use Global Discovery |
| `AzureDevOpsFederated` | No | Cannot use Global Discovery |

**Note**: Service principals and non-interactive auth must use direct environment URLs.

### Environment Resolution

The resolver searches environments in priority order:

1. **GUID ID**: Exact match on `Id`
2. **URL**: Exact match on `ApiUrl` or `Url`
3. **Unique name**: Exact match on `UniqueName`
4. **Friendly name**: Exact match on `FriendlyName`
5. **Partial URL**: Contains match on `ApiUrl` or exact on `UrlName`
6. **Partial friendly name**: Contains match on `FriendlyName`

### Lifecycle

- **Initialization**: Service created from profile or cloud/tenant settings
- **Discovery**: MSAL client initialized, token acquired, API called
- **Cleanup**: `Dispose` unregisters MSAL cache

## Edge Cases

| Scenario | Behavior | Notes |
|----------|----------|-------|
| SPN profile | Throws `NotSupportedException` | Direct URL required |
| Silent auth fails | Falls back to configured interactive method | No automatic fallback between browser/device |
| Browser unavailable | Throws `InvalidOperationException` | User must use device code |
| Multiple matches | Throws `AmbiguousMatchException` | Lists matching environments |
| No matches | Returns null | Caller handles not found |
| Disabled environment | Included with `State = 1` | `IsEnabled = false` |

## Error Handling

| Exception | Condition | Recovery |
|-----------|-----------|----------|
| `NotSupportedException` | Unsupported auth method for discovery | Use direct URL or interactive profile |
| `InvalidOperationException` | Browser unavailable for InteractiveBrowser | Create device code profile |
| `AmbiguousMatchException` | Multiple environments match | Be more specific |
| `MsalUiRequiredException` | Token cache miss | Interactive auth triggered |

## DiscoveredEnvironment Properties

| Property | Type | Description |
|----------|------|-------------|
| `Id` | Guid | OrganizationId |
| `EnvironmentId` | string? | Power Platform environment ID |
| `FriendlyName` | string | Display name |
| `UniqueName` | string | Unique instance name |
| `UrlName` | string? | Subdomain part of URL |
| `ApiUrl` | string | Full API URL (https://org.crm.dynamics.com) |
| `Url` | string? | Web application URL |
| `State` | int | 0 = enabled, 1 = disabled |
| `Version` | string? | Dataverse version |
| `Region` | string? | Geographic region (NAM, EUR, etc.) |
| `TenantId` | Guid? | Azure AD tenant ID |
| `OrganizationType` | int | Environment type code |
| `IsUserSysAdmin` | bool | User has sysadmin role |
| `TrialExpirationDate` | DateTimeOffset? | Trial expiration |

### Computed Properties

| Property | Description |
|----------|-------------|
| `IsEnabled` | State == 0 |
| `IsTrial` | Has trial expiration |
| `EnvironmentType` | Maps type code to string (Production, Sandbox, Trial, etc.) |

### Environment Types

| Code | Type |
|------|------|
| 0 | Production |
| 5, 6 | Sandbox |
| 7 | Preview |
| 9 | TestDrive |
| 11, 14 | Trial |
| 12 | Default |
| 13 | Developer |
| 15 | Teams |

## Configuration

### Global Discovery Endpoints

| Cloud | Endpoint |
|-------|----------|
| Public | `https://globaldisco.crm.dynamics.com/` |
| GCC | `https://globaldisco.crm9.dynamics.com/` |
| GCCHigh | `https://globaldisco.crm.microsoftdynamics.us/` |
| DoD | `https://globaldisco.crm.appsplatform.us/` |
| China | `https://globaldisco.crm.dynamics.cn/` |

## Thread Safety

- **GlobalDiscoveryService**: Not thread-safe (single MSAL client per instance)
- **EnvironmentResolver**: Thread-safe (static, pure functions)
- **DiscoveredEnvironment**: Thread-safe (immutable after creation)

## CapturedHomeAccountId

After discovery, `GlobalDiscoveryService.CapturedHomeAccountId` contains the MSAL account identifier. Callers should persist this to the profile to enable silent auth on subsequent calls:

```csharp
var gds = GlobalDiscoveryService.FromProfile(profile);
var environments = await gds.DiscoverEnvironmentsAsync();

// Persist for next time
if (gds.CapturedHomeAccountId != null)
{
    profile.HomeAccountId = gds.CapturedHomeAccountId;
    await profileStore.SaveAsync(collection);
}
```

## Dependencies

- **Internal**:
  - `PPDS.Auth.Profiles` - Profile configuration
  - `PPDS.Auth.Cloud` - Cloud endpoints
  - `PPDS.Auth.Credentials` - MSAL client, device code info
- **External**:
  - `Microsoft.PowerPlatform.Dataverse.Client` (ServiceClient.DiscoverOnlineOrganizationsAsync)
  - `Microsoft.Identity.Client` (MSAL)
  - `Microsoft.Xrm.Sdk.Discovery` (EndpointType, OrganizationType)

## Related

- [Profile Storage spec](./01-profile-storage.md) - Stores environment binding
- [Token Management spec](./03-token-management.md) - MSAL token caching
- [Cloud Support spec](./05-cloud-support.md) - Cloud-specific discovery endpoints

## Source Files

| File | Purpose |
|------|---------|
| `src/PPDS.Auth/Discovery/IGlobalDiscoveryService.cs` | Discovery interface |
| `src/PPDS.Auth/Discovery/GlobalDiscoveryService.cs` | Discovery implementation |
| `src/PPDS.Auth/Discovery/DiscoveredEnvironment.cs` | Environment model |
| `src/PPDS.Auth/Discovery/EnvironmentResolver.cs` | Resolution logic |
| `src/PPDS.Auth/Discovery/EnvironmentResolutionService.cs` | High-level service |
