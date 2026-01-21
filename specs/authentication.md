# Authentication

**Status:** Implemented
**Version:** 1.0
**Last Updated:** 2026-01-21
**Code:** [src/PPDS.Auth/](../src/PPDS.Auth/)

---

## Overview

The Authentication system provides a PAC CLI-compatible profile model with support for 9 authentication methods, native OS credential storage, and unified token caching across all interfaces. It bridges user identity management with the Connection Pool via the `IConnectionSource` abstraction.

### Goals

- **PAC CLI compatibility**: Two-layer model (WHO = profile, WHERE = environment) familiar to Power Platform developers
- **Multi-method support**: Interactive, service principal, managed identity, and federated identity flows
- **Unified session**: Login from any interface (CLI, TUI, VS Code) available in all interfaces
- **Connection pool integration**: Profiles become connection sources for quota multiplication

### Non-Goals

- Custom identity provider integration (uses MSAL/Azure.Identity)
- Web-based authentication flows (browser-based only)
- Credential rotation automation (out of scope; user manages credential lifecycle)

---

## Architecture

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                              ProfileService                                  │
│   (Create, List, Select, Delete profiles - single code path for all UIs)    │
└────────────────────────────────────┬────────────────────────────────────────┘
                                     │
         ┌───────────────────────────┼───────────────────────────┐
         │                           │                           │
         ▼                           ▼                           ▼
┌─────────────────┐       ┌──────────────────┐       ┌──────────────────────┐
│   ProfileStore  │       │ ProfileResolver  │       │ CredentialProvider   │
│ (profiles.json) │       │ (flag→env→global)│       │     Factory          │
└────────┬────────┘       └──────────────────┘       └──────────┬───────────┘
         │                                                      │
         ▼                                                      ▼
┌─────────────────┐                                  ┌──────────────────────┐
│ NativeCredential│                                  │   9 Auth Providers   │
│     Store       │                                  │ (InteractiveBrowser, │
│ (OS credential  │                                  │  DeviceCode, Client  │
│  manager)       │                                  │  Secret, Cert, MI,   │
└─────────────────┘                                  │  Federated...)       │
                                                     └──────────┬───────────┘
                                                                │
                                                                ▼
                                                     ┌──────────────────────┐
                                                     │ ProfileConnection    │
                                                     │     Source           │
                                                     │ (→IConnectionSource) │
                                                     └──────────────────────┘
                                                                │
                                                                ▼
                                                     ┌──────────────────────┐
                                                     │  Connection Pool     │
                                                     │ (see connection-     │
                                                     │  pool.md)            │
                                                     └──────────────────────┘
```

### Components

| Component | Responsibility |
|-----------|----------------|
| `ProfileStore` | Persist profiles to `profiles.json` with thread-safe caching |
| `ProfileResolver` | Resolve effective profile: explicit → env var → global active |
| `ProfileCollection` | In-memory profile container with active tracking |
| `AuthProfile` | Data model for identity, credentials, environment binding |
| `CredentialProviderFactory` | Route auth method to appropriate provider |
| `ICredentialProvider` | Create authenticated ServiceClient for Dataverse |
| `NativeCredentialStore` | Secure OS credential storage (Windows/macOS/Linux) |
| `GlobalDiscoveryService` | Environment discovery via Power Platform API |
| `ProfileConnectionSource` | Bridge profiles to connection pool via `IConnectionSource` |

### Dependencies

- Uses patterns from: [architecture.md](./architecture.md)
- Consumed by: [connection-pool.md](./connection-pool.md) via `IConnectionSource`

---

## Specification

### Core Requirements

1. **Profile storage**: JSON file at `~/.ppds/profiles.json` (Windows: `%LOCALAPPDATA%\PPDS`)
2. **Session isolation**: TUI profile switching is session-only; `ppds auth select` changes global default
3. **Token cache sharing**: MSAL token cache at `~/.ppds/msal_token_cache.bin` shared across interfaces
4. **HomeAccountId persistence**: After authentication, persist `HomeAccountId` for silent token acquisition
5. **Secure credential storage**: Service principal secrets stored in OS credential manager, not profiles.json

### Primary Flows

**Profile Creation:**

1. **Validate request** ([`ProfileService.cs:225-227`](../src/PPDS.Cli/Services/Profile/ProfileService.cs#L225-L227)): Check name uniqueness, auth method compatibility
2. **Create AuthProfile** ([`ProfileService.cs:255-264`](../src/PPDS.Cli/Services/Profile/ProfileService.cs#L255-L264)): Assign index, set auth method and cloud
3. **Store credentials** ([`ProfileService.cs:268-282`](../src/PPDS.Cli/Services/Profile/ProfileService.cs#L268-L282)): ClientSecret/CertificatePassword to NativeCredentialStore
4. **Authenticate** ([`ProfileService.cs:313-318`](../src/PPDS.Cli/Services/Profile/ProfileService.cs#L313-L318)): Create credential provider, get ServiceClient
5. **Populate profile** ([`ProfileService.cs:320-339`](../src/PPDS.Cli/Services/Profile/ProfileService.cs#L320-L339)): Extract Username, ObjectId, HomeAccountId from auth result
6. **Resolve environment** ([`ProfileService.cs:341-345`](../src/PPDS.Cli/Services/Profile/ProfileService.cs#L341-L345)): Via GlobalDiscoveryService or direct connection
7. **Persist** ([`ProfileService.cs:354-355`](../src/PPDS.Cli/Services/Profile/ProfileService.cs#L354-L355)): Add to collection, save to disk

**Profile Resolution:**

1. **Explicit parameter**: CLI `--profile` flag or API parameter (highest priority)
2. **Environment variable**: `PPDS_PROFILE` (enables per-terminal profile)
3. **Global active**: `ProfileCollection.ActiveProfile` from `profiles.json` (lowest priority)

Resolution implemented in [`ProfileResolver.cs:32-48`](../src/PPDS.Auth/Profiles/ProfileResolver.cs#L32-L48).

**Connection Source Creation:**

1. **Load profile** ([`ProfileServiceFactory.cs:70-85`](../src/PPDS.Cli/Infrastructure/ProfileServiceFactory.cs#L70-L85)): From ProfileStore
2. **Create ProfileConnectionSource** ([`ProfileServiceFactory.cs:118`](../src/PPDS.Cli/Infrastructure/ProfileServiceFactory.cs#L118)): With profile, environment URL, callbacks
3. **Lazy authentication** ([`ProfileConnectionSource.cs:149-223`](../src/PPDS.Auth/Pooling/ProfileConnectionSource.cs#L149-L223)): Deferred until `GetSeedClient()` called
4. **Persist HomeAccountId** ([`ProfileConnectionSource.cs:198`](../src/PPDS.Auth/Pooling/ProfileConnectionSource.cs#L198)): Callback updates profile for silent auth

### Constraints

- Profile names: max 30 characters, alphanumeric with spaces/underscores/hyphens
- First profile auto-selected as active
- Service principals cannot use Global Discovery Service (no delegated permissions)
- Certificate auth requires private key access

### Authentication Methods

| Method | Use Case | MSAL | Token Cache |
|--------|----------|------|-------------|
| `InteractiveBrowser` | Desktop default | Yes | File-based |
| `DeviceCode` | Headless/SSH | Yes | File-based |
| `ClientSecret` | Production server | No | None |
| `CertificateFile` | Secure SPN (PFX) | No | None |
| `CertificateStore` | Windows cert store | No | None |
| `ManagedIdentity` | Azure workloads | No | Runtime |
| `GitHubFederated` | GitHub Actions OIDC | No | Runtime |
| `AzureDevOpsFederated` | Azure Pipelines OIDC | No | Runtime |
| `UsernamePassword` | Legacy/test (ROPC) | Yes | File-based |

---

## Core Types

### AuthProfile

Profile data model storing identity, credentials references, and environment binding ([`AuthProfile.cs:10-286`](../src/PPDS.Auth/Profiles/AuthProfile.cs#L10-L286)).

```csharp
public sealed class AuthProfile
{
    public int Index { get; set; }
    public string? Name { get; set; }
    public AuthMethod AuthMethod { get; set; }
    public CloudEnvironment Cloud { get; set; }
    public string? TenantId { get; set; }
    public string? HomeAccountId { get; set; }
    public EnvironmentInfo? Environment { get; set; }
}
```

The `HomeAccountId` property (format: `{objectId}.{tenantId}`) enables MSAL to locate cached tokens across sessions.

### ICredentialProvider

Interface for authentication methods ([`ICredentialProvider.cs:12-85`](../src/PPDS.Auth/Credentials/ICredentialProvider.cs#L12-L85)).

```csharp
public interface ICredentialProvider
{
    AuthMethod AuthMethod { get; }
    string? Identity { get; }
    string? HomeAccountId { get; }
    Task<ServiceClient> CreateServiceClientAsync(string environmentUrl, CancellationToken ct);
}
```

### IConnectionSource

Bridge to connection pool ([`IConnectionSource.cs:20-63`](../src/PPDS.Dataverse/Pooling/IConnectionSource.cs#L20-L63)).

```csharp
public interface IConnectionSource : IDisposable
{
    string Name { get; }
    ServiceClient GetSeedClient();
    void InvalidateSeed();
}
```

`ProfileConnectionSource` implements this interface, enabling authenticated profiles to serve as connection pool sources.

### Usage Pattern

```csharp
// Single profile → single connection source
await using var provider = await ProfileServiceFactory.CreateFromProfileAsync(
    profileName: "Production",
    progress: reporter,
    ct: ct);

var pool = provider.GetRequiredService<IDataverseConnectionPool>();
await using var client = await pool.GetClientAsync();

// Multiple profiles → quota multiplication
await using var provider = await ProfileServiceFactory.CreateFromProfilesAsync(
    profileNames: "AppUser1,AppUser2,AppUser3",
    progress: reporter,
    ct: ct);
```

---

## Credential Storage

### profiles.json Schema (v2)

```json
{
  "version": 2,
  "activeProfileIndex": 1,
  "profiles": [
    {
      "index": 1,
      "name": "Production",
      "authMethod": "interactiveBrowser",
      "cloud": "public",
      "tenantId": "00000000-0000-0000-0000-000000000000",
      "username": "user@contoso.com",
      "homeAccountId": "11111111.00000000-0000-0000-0000-000000000000",
      "environment": {
        "url": "https://org.crm.dynamics.com/",
        "displayName": "Production",
        "type": "Production"
      }
    }
  ]
}
```

**Note:** ClientSecret, CertificatePassword, and UsernamePassword credentials are NOT stored in profiles.json—they use `NativeCredentialStore`.

### NativeCredentialStore

Platform-native secure storage ([`NativeCredentialStore.cs:1-329`](../src/PPDS.Auth/Credentials/NativeCredentialStore.cs#L1-L329)).

| Platform | Backend | Security |
|----------|---------|----------|
| Windows | Credential Manager | DPAPI (CurrentUser scope) |
| macOS | Keychain Services | Secure Enclave |
| Linux | libsecret | GNOME Keyring / KWallet |

Credentials stored with service name `"ppds.credentials"` and keyed by ApplicationId.

### CI/CD Fallback

For headless Linux without libsecret, use `--accept-cleartext-caching` flag (parity with PAC CLI) or set credentials via environment variable:

```bash
export PPDS_SPN_SECRET="your-client-secret"
ppds data export account --profile MyServicePrincipal
```

---

## Global Discovery Service

Environment discovery for interactive users ([`GlobalDiscoveryService.cs:1-319`](../src/PPDS.Auth/Discovery/GlobalDiscoveryService.cs#L1-L319)).

### Supported Auth Methods

Only delegated user authentication supports Global Discovery:
- `InteractiveBrowser`
- `DeviceCode`

Service principals, managed identities, and federated credentials must use direct environment URLs.

### Cloud Endpoints

| Cloud | Discovery URL |
|-------|---------------|
| Public | `https://globaldisco.crm.dynamics.com` |
| UsGov | `https://globaldisco.crm9.dynamics.com` |
| UsGovHigh | `https://globaldisco.crm.microsoftdynamics.us` |
| UsGovDod | `https://globaldisco.crm.appsplatform.us` |
| China | `https://globaldisco.crm.dynamics.cn` |

### Resolution Strategy

`EnvironmentResolver` ([`EnvironmentResolver.cs:19-89`](../src/PPDS.Auth/Discovery/EnvironmentResolver.cs#L19-L89)) uses multi-pass matching:

1. Exact GUID match
2. Exact URL match (ApiUrl or Url)
3. Exact UniqueName match
4. Exact FriendlyName match
5. Partial URL match (subdomain)
6. Partial FriendlyName match

Throws `AmbiguousMatchException` if multiple environments match.

---

## Error Handling

### Error Types

| Error | Condition | Recovery |
|-------|-----------|----------|
| `AuthenticationException` | MSAL/Azure.Identity failure | Check credentials, re-authenticate |
| `PpdsAuthException` | Profile not found, no active | Specify profile or create one |
| `CredentialUnavailableException` | Managed identity not in Azure | Use different auth method |
| `MsalUiRequiredException` | Silent auth failed, MFA required | Re-authenticate interactively |

### Recovery Strategies

- **Token expired**: Credential provider attempts silent refresh; if fails, prompts for interactive auth
- **Service principal secret invalid**: Remove from credential store, re-create profile
- **Certificate not found**: Verify file path or thumbprint, check private key access

### Edge Cases

| Scenario | Expected Behavior |
|----------|-------------------|
| First profile created | Auto-selected as active |
| Profile deleted while active | Next profile becomes active, or null if last |
| TUI profile switch | Session-only; global unchanged |
| HomeAccountId missing | Silent auth fails, prompts interactive |

---

## Design Decisions

### Why PAC CLI-Compatible Profile Model?

**Context:** Power Platform developers use PAC CLI with its two-layer model (profile = WHO, environment = WHERE).

**Decision:** Adopt PAC CLI's profile model for familiar experience.

**Key behaviors matching PAC:**
- Profiles can be named or unnamed (referenced by index)
- First profile auto-selected as active
- Environment optional ("universal" profiles)
- `env select` binds environment to current profile

**Alternatives considered:**
- Single connection string per profile: Rejected - no environment discovery
- Per-command authentication: Rejected - poor CI/CD experience

**Consequences:**
- Positive: Familiar to PAC CLI users; enables profile reuse across environments
- Negative: Additional complexity vs simple connection string

### Why Session Isolation?

**Context:** TUI profile switching originally updated global `profiles.json`, causing cross-session interference.

**Decision:** TUI switching is session-only; `ppds auth select` explicitly changes global default.

**Resolution order:**
```
1. Explicit (--profile flag, API parameter)    [highest]
2. PPDS_PROFILE environment variable
3. Global active profile from profiles.json   [lowest]
```

**Use cases:**
| Consumer | Profile Source | Updates Global? |
|----------|---------------|-----------------|
| CLI commands | Flag → env var → global | No (reads only) |
| `ppds auth select` | N/A | **Yes** (explicit) |
| TUI | Session-only switching | **No** |
| `ppds serve` RPC | Per-client session | **No** |

**Alternatives considered:**
- Global switch from TUI: Rejected - disrupted concurrent CLI sessions

**Consequences:**
- Positive: Independent sessions; CI/CD unaffected by interactive use
- Negative: TUI doesn't remember profile across restarts

### Why Persist HomeAccountId?

**Context:** MSAL token cache keys tokens by `HomeAccountId`. Without persistence, each session forced re-authentication.

**Decision:** Persist `HomeAccountId` to profile after successful authentication.

**Verification:**
1. Delete token cache, run `ppds auth who` (prompts for auth)
2. Check `profiles.json` - `homeAccountId` now populated
3. Run `ppds auth who` again - no prompt (uses cached token)
4. Start TUI, switch profiles - no prompt if token cached

**Alternatives considered:**
- Username-based lookup: Rejected - ambiguous for multi-tenant

**Consequences:**
- Positive: Single sign-on across all interfaces; profile switching without re-auth
- Negative: Slightly more file I/O (profile saved after each auth)

### Why Native OS Credential Storage?

**Context:** Original implementation misused `MsalCacheHelper` and wrote credentials as plaintext JSON.

**Decision:** Use native OS credential managers via `Devlooped.CredentialManager` (Git Credential Manager infrastructure).

| Platform | Store | Security |
|----------|-------|----------|
| Windows | Credential Manager | DPAPI with CurrentUser scope |
| macOS | Keychain Services | Secure Enclave integration |
| Linux | libsecret | GNOME Keyring / KWallet |

**Alternatives considered:**
- DPAPI file encryption: Rejected - Windows-only
- Custom AES encryption: Rejected - key management complexity

**Consequences:**
- Positive: OS-managed encryption; proven implementations; cross-platform
- Negative: Linux requires libsecret (CI/CD uses plaintext fallback)

---

## Extension Points

### Adding a Credential Provider

1. **Implement `ICredentialProvider`** ([`ICredentialProvider.cs`](../src/PPDS.Auth/Credentials/ICredentialProvider.cs))
2. **Add `AuthMethod` enum value** ([`AuthMethod.cs`](../src/PPDS.Auth/Profiles/AuthMethod.cs))
3. **Register in factory** ([`CredentialProviderFactory.cs:49-80`](../src/PPDS.Auth/Credentials/CredentialProviderFactory.cs#L49-L80))

**Example skeleton:**

```csharp
public sealed class MyCredentialProvider : ICredentialProvider
{
    public AuthMethod AuthMethod => AuthMethod.MyMethod;
    public string? Identity => _identity;
    public string? HomeAccountId => _homeAccountId;

    public async Task<ServiceClient> CreateServiceClientAsync(
        string environmentUrl, CancellationToken ct)
    {
        // Acquire token via your auth mechanism
        var token = await AcquireTokenAsync(ct);

        // Create ServiceClient with token provider
        return new ServiceClient(new ConnectionOptions
        {
            ServiceUri = new Uri(environmentUrl),
            AccessTokenProviderFunctionAsync = (_, _) => Task.FromResult(token)
        });
    }
}
```

### Adding a Cloud Environment

1. **Add enum value** to `CloudEnvironment` ([`CloudEnvironment.cs`](../src/PPDS.Auth/Cloud/CloudEnvironment.cs))
2. **Add endpoints** to `CloudEndpoints` ([`CloudEndpoints.cs`](../src/PPDS.Auth/Cloud/CloudEndpoints.cs)):
   - `GetAuthorityBaseUrl()` - MSAL login endpoint
   - `GetGlobalDiscoveryUrl()` - GDS endpoint
   - `GetAzureCloudInstance()` - MSAL cloud instance

---

## Configuration

| Setting | Type | Default | Description |
|---------|------|---------|-------------|
| `PPDS_CONFIG_DIR` | env var | `~/.ppds` | Override data directory |
| `PPDS_PROFILE` | env var | (none) | Override active profile per-terminal |
| `PPDS_SPN_SECRET` | env var | (none) | Service principal secret (bypasses store) |
| `--accept-cleartext-caching` | flag | false | Allow plaintext credential storage on Linux |

---

## Testing

### Acceptance Criteria

- [ ] Login from CLI available in TUI (shared token cache)
- [ ] TUI profile switch doesn't affect concurrent CLI session
- [ ] Service principal secrets stored in OS credential manager
- [ ] HomeAccountId enables silent token acquisition
- [ ] Multiple profiles can be used for connection pool quota multiplication

### Edge Cases

| Scenario | Input | Expected Output |
|----------|-------|-----------------|
| Profile name collision | `ppds auth create --name existing` | Error: name in use |
| Service principal + GDS | SPN profile, `env list` | Error: GDS requires interactive |
| Token expired + silent | Cached token past expiry | Auto-refresh or interactive prompt |
| Linux without libsecret | No keyring, no flag | Error with hint for `--accept-cleartext-caching` |

### Test Examples

```csharp
[Fact]
public async Task CreateProfile_PersistsHomeAccountId()
{
    // Arrange
    var store = new ProfileStore(testPath, logger);

    // Act - Create profile (triggers auth)
    await profileService.CreateProfileAsync(new ProfileCreateRequest
    {
        UseDeviceCode = true,
        Environment = "https://org.crm.dynamics.com"
    });

    // Assert - HomeAccountId captured
    var collection = await store.LoadAsync();
    Assert.NotNull(collection.Profiles[0].HomeAccountId);
}

[Fact]
public async Task ProfileResolver_PrefersExplicitOverGlobal()
{
    // Arrange
    Environment.SetEnvironmentVariable("PPDS_PROFILE", "EnvProfile");

    // Act
    var result = ProfileResolver.GetEffectiveProfileName("ExplicitProfile");

    // Assert - Explicit wins
    Assert.Equal("ExplicitProfile", result);
}
```

---

## Related Specs

- [architecture.md](./architecture.md) - Shared local state, Application Services pattern
- [connection-pool.md](./connection-pool.md) - Consumes `IConnectionSource` from profiles

---

## Roadmap

- TUI session persistence (`~/.ppds/tui-state.json`) for remembered profile
- Environment theming and safety warnings (visual distinction for PROD)
- Additional federated identity providers (AWS, GCP)
