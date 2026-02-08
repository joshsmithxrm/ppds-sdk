# Fix Interactive & Device Code Auth Bugs - Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Fix two auth bugs: token cache scope mismatch in DeviceCodeCredentialProvider, and CredentialProviderFactory ignoring user's explicit DeviceCode choice.

**Architecture:** Port the `_cachedResultUrl` pattern from `InteractiveBrowserCredentialProvider` (PR #515) to `DeviceCodeCredentialProvider`. Fix `CredentialProviderFactory` to map `AuthMethod.DeviceCode` directly to `DeviceCodeCredentialProvider` instead of routing through `CreateInteractiveProvider()`.

**Tech Stack:** C# / .NET 8+, MSAL, xUnit, FluentAssertions

---

## Bug Analysis

### Bug 1: Token cache scope mismatch in DeviceCodeCredentialProvider

**Introduced:** Commit `3e1289b` (Dec 29, 2025), PR #28 — 41 days ago
**Partially fixed (browser only):** Commit `82ebfc7` (Feb 7, 2026), PR #515

**Root cause:** `DeviceCodeCredentialProvider.GetTokenAsync()` caches tokens in `_cachedResult` but does not track which environment URL the token was obtained for. During profile creation, the initial auth against `globaldisco` caches a token, and the subsequent environment validation reuses that globaldisco-scoped token for the target environment URL, causing "Server Error, no error report generated from server".

**Evidence:** `InteractiveBrowserCredentialProvider` was fixed with `_cachedResultUrl` in PR #515, but `DeviceCodeCredentialProvider` was not updated.

### Bug 2: CredentialProviderFactory ignores user's explicit DeviceCode choice

**Introduced:** Commit `3e1289b` (Dec 29, 2025), PR #28 — 41 days ago

**Root cause:** `CredentialProviderFactory` maps `AuthMethod.DeviceCode` to `CreateInteractiveProvider()`, which checks `InteractiveBrowserCredentialProvider.IsAvailable()` and returns `InteractiveBrowserCredentialProvider` when a browser is available. This overrides the user's explicit selection of Device Code auth.

**Evidence:**
```csharp
// CredentialProviderFactory.cs - BEFORE fix
AuthMethod.DeviceCode => CreateInteractiveProvider(profile, deviceCodeCallback, beforeInteractiveAuth),
// CreateInteractiveProvider returns InteractiveBrowserCredentialProvider when browser available
```

---

## Tasks

### Task 1: Write failing test for DeviceCodeCredentialProvider token cache URL tracking

**Files:**
- Create: `tests/PPDS.Auth.Tests/Credentials/DeviceCodeCredentialProviderTests.cs`

**Step 1: Write the failing test**

```csharp
// tests/PPDS.Auth.Tests/Credentials/DeviceCodeCredentialProviderTests.cs
using FluentAssertions;
using PPDS.Auth.Cloud;
using PPDS.Auth.Credentials;
using PPDS.Auth.Profiles;
using Xunit;

namespace PPDS.Auth.Tests.Credentials;

public class DeviceCodeCredentialProviderTests
{
    [Fact]
    public void Constructor_WithDefaults_DoesNotThrow()
    {
        using var provider = new DeviceCodeCredentialProvider();
        provider.Should().NotBeNull();
        provider.AuthMethod.Should().Be(AuthMethod.DeviceCode);
    }

    [Fact]
    public void Constructor_WithAllParameters_DoesNotThrow()
    {
        using var provider = new DeviceCodeCredentialProvider(
            cloud: CloudEnvironment.Public,
            tenantId: "test-tenant",
            username: "user@example.com",
            homeAccountId: "account-id",
            deviceCodeCallback: _ => { });
        provider.Should().NotBeNull();
    }

    [Fact]
    public void FromProfile_CreatesProviderWithProfileSettings()
    {
        var profile = new AuthProfile
        {
            AuthMethod = AuthMethod.DeviceCode,
            Cloud = CloudEnvironment.UsGov,
            TenantId = "gov-tenant",
            Username = "gov-user@example.com",
            HomeAccountId = "gov-account-id"
        };
        using var provider = DeviceCodeCredentialProvider.FromProfile(profile);
        provider.Should().NotBeNull();
        provider.AuthMethod.Should().Be(AuthMethod.DeviceCode);
    }

    [Fact]
    public void HasCachedResultUrlField_ForTokenScopeMismatchPrevention()
    {
        var field = typeof(DeviceCodeCredentialProvider)
            .GetField("_cachedResultUrl",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        field.Should().NotBeNull(
            because: "DeviceCodeCredentialProvider must track the URL associated with cached tokens " +
                     "to prevent token scope mismatch (same fix as InteractiveBrowserCredentialProvider PR #515)");
    }
}
```

**Step 2: Run test to verify it fails**

Run: `dotnet test tests/PPDS.Auth.Tests --filter "FullyQualifiedName~DeviceCodeCredentialProviderTests.HasCachedResultUrlField" --framework net9.0`
Expected: FAIL — `_cachedResultUrl` field does not exist

### Task 2: Write failing test for CredentialProviderFactory DeviceCode mapping

**Files:**
- Modify: `tests/PPDS.Auth.Tests/Credentials/CredentialProviderFactoryTests.cs`

**Step 1: Add the failing test**

```csharp
// Add to CredentialProviderFactoryTests.cs, before the IsSupported_ValidAuthMethod_ReturnsTrue test
[Fact]
public void Create_DeviceCode_ReturnsDeviceCodeProvider()
{
    var profile = new AuthProfile { AuthMethod = AuthMethod.DeviceCode };

    var provider = CredentialProviderFactory.Create(profile);

    provider.Should().BeOfType<DeviceCodeCredentialProvider>(
        because: "when user explicitly selects DeviceCode, their choice must be respected");
    provider.Dispose();
}
```

**Step 2: Run test to verify it fails**

Run: `dotnet test tests/PPDS.Auth.Tests --filter "FullyQualifiedName~CredentialProviderFactoryTests.Create_DeviceCode_ReturnsDeviceCodeProvider" --framework net9.0`
Expected: FAIL — "Expected type to be DeviceCodeCredentialProvider but found InteractiveBrowserCredentialProvider"

### Task 3: Fix DeviceCodeCredentialProvider — add `_cachedResultUrl` tracking

**Files:**
- Modify: `src/PPDS.Auth/Credentials/DeviceCodeCredentialProvider.cs:29-30` (add field)
- Modify: `src/PPDS.Auth/Credentials/DeviceCodeCredentialProvider.cs:165-168` (check URL on cache hit)
- Modify: `src/PPDS.Auth/Credentials/DeviceCodeCredentialProvider.cs:184-188` (track URL on silent acquisition)
- Modify: `src/PPDS.Auth/Credentials/DeviceCodeCredentialProvider.cs:230-232` (track URL on device code acquisition)
- Modify: `src/PPDS.Auth/Credentials/DeviceCodeCredentialProvider.cs:277-279` (check URL in GetCachedTokenInfoAsync)
- Modify: `src/PPDS.Auth/Credentials/DeviceCodeCredentialProvider.cs:303-305` (track URL in GetCachedTokenInfoAsync)

**Step 1: Add `_cachedResultUrl` field**

After line 29 (`private AuthenticationResult? _cachedResult;`), add:
```csharp
private string? _cachedResultUrl;
```

**Step 2: Update `GetTokenAsync` in-memory cache check to validate URL**

Change the cache check from:
```csharp
if (_cachedResult != null && _cachedResult.ExpiresOn > DateTimeOffset.UtcNow.AddMinutes(5))
```
To:
```csharp
if (_cachedResult != null
    && _cachedResult.ExpiresOn > DateTimeOffset.UtcNow.AddMinutes(5)
    && string.Equals(_cachedResultUrl, environmentUrl, StringComparison.OrdinalIgnoreCase))
```

**Step 3: Track URL after silent acquisition**

After `_cachedResult = await _msalClient!.AcquireTokenSilent(...)`, add:
```csharp
_cachedResultUrl = environmentUrl;
```

**Step 4: Track URL after device code acquisition**

After `.ExecuteAsync(cancellationToken).ConfigureAwait(false);` in the device code flow, add:
```csharp
_cachedResultUrl = environmentUrl;
```

**Step 5: Update `GetCachedTokenInfoAsync` in-memory cache check**

Change from:
```csharp
if (_cachedResult != null)
```
To:
```csharp
if (_cachedResult != null
    && string.Equals(_cachedResultUrl, environmentUrl, StringComparison.OrdinalIgnoreCase))
```

**Step 6: Track URL in `GetCachedTokenInfoAsync` after silent acquisition**

After `_cachedResult = result;`, add:
```csharp
_cachedResultUrl = environmentUrl;
```

**Step 7: Run test to verify it passes**

Run: `dotnet test tests/PPDS.Auth.Tests --filter "FullyQualifiedName~DeviceCodeCredentialProviderTests" --framework net9.0`
Expected: PASS — all 4 tests pass

### Task 4: Fix CredentialProviderFactory — map DeviceCode directly

**Files:**
- Modify: `src/PPDS.Auth/Credentials/CredentialProviderFactory.cs:65` (CreateAsync)
- Modify: `src/PPDS.Auth/Credentials/CredentialProviderFactory.cs:109` (Create)
- Modify: `src/PPDS.Auth/Credentials/CredentialProviderFactory.cs:215-228` (remove dead CreateInteractiveProvider)

**Step 1: Fix both `CreateAsync` and `Create` switch arms**

Change both from:
```csharp
AuthMethod.DeviceCode => CreateInteractiveProvider(profile, deviceCodeCallback, beforeInteractiveAuth),
```
To:
```csharp
AuthMethod.DeviceCode => DeviceCodeCredentialProvider.FromProfile(profile, deviceCodeCallback),
```

**Step 2: Remove dead `CreateInteractiveProvider` method**

Delete the entire method (was at lines 212-228):
```csharp
// DELETE THIS:
private static ICredentialProvider CreateInteractiveProvider(
    AuthProfile profile,
    Action<DeviceCodeInfo>? deviceCodeCallback,
    Func<Action<DeviceCodeInfo>?, PreAuthDialogResult>? beforeInteractiveAuth)
{
    if (InteractiveBrowserCredentialProvider.IsAvailable())
    {
        return InteractiveBrowserCredentialProvider.FromProfile(profile, deviceCodeCallback, beforeInteractiveAuth);
    }
    else
    {
        return DeviceCodeCredentialProvider.FromProfile(profile, deviceCodeCallback);
    }
}
```

**Step 3: Run test to verify it passes**

Run: `dotnet test tests/PPDS.Auth.Tests --filter "FullyQualifiedName~CredentialProviderFactoryTests.Create_DeviceCode_ReturnsDeviceCodeProvider" --framework net9.0`
Expected: PASS

### Task 5: Run full test suite

**Step 1: Run all auth tests**

Run: `dotnet test tests/PPDS.Auth.Tests --framework net9.0`
Expected: All 398 tests pass

**Step 2: Run full unit test suite**

Run: `dotnet test --filter "Category!=Integration" --framework net9.0`
Expected: All ~4,261 tests pass, 0 failures

### Task 6: Commit

```bash
git add src/PPDS.Auth/Credentials/DeviceCodeCredentialProvider.cs src/PPDS.Auth/Credentials/CredentialProviderFactory.cs tests/PPDS.Auth.Tests/Credentials/DeviceCodeCredentialProviderTests.cs tests/PPDS.Auth.Tests/Credentials/CredentialProviderFactoryTests.cs
git commit -m "fix(auth): token cache scope mismatch in device code and factory mapping bug

Port _cachedResultUrl fix from InteractiveBrowserCredentialProvider (PR #515)
to DeviceCodeCredentialProvider. Without URL tracking, a token obtained for
globaldisco was incorrectly reused for the target environment URL.

Fix CredentialProviderFactory to map AuthMethod.DeviceCode directly to
DeviceCodeCredentialProvider instead of routing through CreateInteractiveProvider
which overrode the user's explicit choice with InteractiveBrowserCredentialProvider.

Both bugs introduced in 3e1289b (Dec 29, 2025), PR #28.

Co-Authored-By: Claude Opus 4.6 <noreply@anthropic.com>"
```
