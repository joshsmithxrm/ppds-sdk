# PPDS.Auth: Cloud Support

## Overview

The Cloud Support subsystem provides endpoint resolution for all Azure cloud environments, including Public, US Government (GCC, GCC High, DoD), and China sovereign clouds. It maps cloud identifiers to the correct authentication authorities, discovery services, Power Platform APIs, and Dataverse URLs.

## Public API

### Enums

| Enum | Purpose |
|------|---------|
| `CloudEnvironment` | Azure cloud environments (Public, UsGov, UsGovHigh, UsGovDod, China) |

### Classes

| Class | Purpose |
|-------|---------|
| `CloudEndpoints` | Static methods for endpoint resolution |

## CloudEnvironment Values

| Value | Description | Authority Base URL |
|-------|-------------|-------------------|
| `Public` | Azure Public Cloud (default) | `https://login.microsoftonline.com` |
| `UsGov` | Azure US Government (GCC) | `https://login.microsoftonline.us` |
| `UsGovHigh` | Azure US Government High | `https://login.microsoftonline.us` |
| `UsGovDod` | Azure US Government DoD | `https://login.microsoftonline.us` |
| `China` | Azure China (21Vianet) | `https://login.chinacloudapi.cn` |

## Endpoint Resolution

### CloudEndpoints Methods

| Method | Returns | Description |
|--------|---------|-------------|
| `GetAuthorityUrl` | string | Full authority URL with tenant |
| `GetAuthorityBaseUrl` | string | Authority base URL (no tenant) |
| `GetAzureCloudInstance` | AzureCloudInstance | MSAL cloud instance enum |
| `GetAuthorityHost` | Uri | Azure.Identity authority host |
| `GetGlobalDiscoveryUrl` | string | Global Discovery Service URL |
| `GetPowerAppsApiUrl` | string | Power Apps REST API base URL |
| `GetPowerAutomateApiUrl` | string | Power Automate REST API base URL |
| `GetPowerAppsServiceScope` | string | Token scope for Flow/Connections API |
| `Parse` | CloudEnvironment | Parse string to enum |

## Endpoint Mapping

### Authentication Authority

| Cloud | Authority URL |
|-------|---------------|
| Public | `https://login.microsoftonline.com/{tenant}` |
| UsGov | `https://login.microsoftonline.us/{tenant}` |
| UsGovHigh | `https://login.microsoftonline.us/{tenant}` |
| UsGovDod | `https://login.microsoftonline.us/{tenant}` |
| China | `https://login.chinacloudapi.cn/{tenant}` |

### Global Discovery Service

| Cloud | Discovery URL |
|-------|---------------|
| Public | `https://globaldisco.crm.dynamics.com` |
| UsGov | `https://globaldisco.crm9.dynamics.com` |
| UsGovHigh | `https://globaldisco.crm.microsoftdynamics.us` |
| UsGovDod | `https://globaldisco.crm.appsplatform.us` |
| China | `https://globaldisco.crm.dynamics.cn` |

### Power Apps API

| Cloud | API URL |
|-------|---------|
| Public | `https://api.powerapps.com` |
| UsGov | `https://gov.api.powerapps.us` |
| UsGovHigh | `https://high.api.powerapps.us` |
| UsGovDod | `https://api.apps.appsplatform.us` |
| China | `https://api.powerapps.cn` |

### Power Automate API

| Cloud | API URL |
|-------|---------|
| Public | `https://api.flow.microsoft.com` |
| UsGov | `https://gov.api.flow.microsoft.us` |
| UsGovHigh | `https://high.api.flow.microsoft.us` |
| UsGovDod | `https://api.flow.appsplatform.us` |
| China | `https://api.flow.microsoft.cn` |

### Power Apps Service Scope

Used for Flow API and Connections API token acquisition:

| Cloud | Service Scope |
|-------|---------------|
| Public | `https://service.powerapps.com` |
| UsGov | `https://service.powerapps.us` |
| UsGovHigh | `https://high.service.powerapps.us` |
| UsGovDod | `https://service.apps.appsplatform.us` |
| China | `https://service.powerapps.cn` |

### Dataverse Environment URL Patterns

| Cloud | URL Pattern |
|-------|-------------|
| Public | `https://{org}.crm.dynamics.com` |
| UsGov (GCC) | `https://{org}.crm9.dynamics.com` |
| UsGovHigh | `https://{org}.crm.microsoftdynamics.us` |
| UsGovDod | `https://{org}.crm.appsplatform.us` |
| China | `https://{org}.crm.dynamics.cn` |

**Note**: Dataverse URLs use various region codes in Public cloud:
- `crm.dynamics.com` - North America
- `crm2.dynamics.com` - South America
- `crm3.dynamics.com` - Canada
- `crm4.dynamics.com` - EMEA
- `crm5.dynamics.com` - APAC
- `crm6.dynamics.com` - Australia
- `crm7.dynamics.com` - Japan
- `crm8.dynamics.com` - India
- `crm11.dynamics.com` - UK
- `crm12.dynamics.com` - France
- `crm14.dynamics.com` - UAE
- `crm17.dynamics.com` - Switzerland
- `crm19.dynamics.com` - Germany
- `crm20.dynamics.com` - Norway
- `crm21.dynamics.com` - Korea

## Behaviors

### Authority URL Construction

```
{base_url}/{tenant}
```

Where `tenant` is:
- The specific tenant ID (GUID or domain)
- `"organizations"` for multi-tenant (default when null)
- `"consumers"` for personal accounts (not used by Dataverse)

### Parsing

The `Parse` method accepts:
- `"public"`, `"usgov"`, `"usgovhigh"`, `"usgovdod"`, `"china"` (case-insensitive)
- Empty or null → defaults to `Public`

## Error Handling

| Exception | Condition | Notes |
|-----------|-----------|-------|
| `ArgumentOutOfRangeException` | Unknown cloud value | Defensive coding |
| `ArgumentException` | Invalid string for `Parse` | Lists valid values |

## Thread Safety

- **CloudEndpoints**: Thread-safe (all static pure functions)
- **CloudEnvironment**: Enum values are inherently thread-safe

## Usage Patterns

### Profile Configuration

```csharp
var profile = new AuthProfile
{
    Cloud = CloudEnvironment.UsGovHigh,
    TenantId = "tenant-id"
};
```

### Credential Provider

```csharp
var authority = CloudEndpoints.GetAuthorityUrl(profile.Cloud, profile.TenantId);
// For UsGovHigh: "https://login.microsoftonline.us/tenant-id"
```

### Discovery

```csharp
var discoveryUrl = CloudEndpoints.GetGlobalDiscoveryUrl(profile.Cloud);
// For UsGovHigh: "https://globaldisco.crm.microsoftdynamics.us"
```

### Power Platform Token

```csharp
var scope = CloudEndpoints.GetPowerAppsServiceScope(profile.Cloud);
// For UsGovHigh: "https://high.service.powerapps.us"
```

## Dependencies

- **Internal**: None (leaf component)
- **External**:
  - `Microsoft.Identity.Client` (AzureCloudInstance)
  - `Azure.Identity` (AzureAuthorityHosts)

## Related

- [Profile Storage spec](./01-profile-storage.md) - Stores cloud preference
- [Credential Providers spec](./02-credential-providers.md) - Uses cloud for authority
- [Token Management spec](./03-token-management.md) - Uses cloud for MSAL config
- [Environment Discovery spec](./04-environment-discovery.md) - Uses cloud for discovery URL

## Source Files

| File | Purpose |
|------|---------|
| `src/PPDS.Auth/Cloud/CloudEnvironment.cs` | Cloud environment enum |
| `src/PPDS.Auth/Cloud/CloudEndpoints.cs` | Endpoint resolution |
