# PPDS Unified CLI - Implementation Progress

**Spec:** [UNIFIED_CLI_SPEC.md](UNIFIED_CLI_SPEC.md)
**ADR:** [ADR-0008](../adr/0008_UNIFIED_CLI_AND_AUTH.md)
**Branch:** `feature/unified-cli`
**Started:** 2025-01-27

---

## Phase Overview

| Phase | Description | Status | Dependencies |
|-------|-------------|--------|--------------|
| 1 | PPDS.Auth Foundation | Not Started | - |
| 2 | CLI Restructure | Not Started | Phase 1 |
| 3 | Auth Commands | Not Started | Phase 1, 2 |
| 4 | Environment Discovery | Not Started | Phase 1, 3 |
| 5 | Additional Auth Methods | Not Started | Phase 3 |
| 6 | Data Command Integration | Not Started | Phase 3, 4 |
| 7 | Pooling Support | Not Started | Phase 6 |
| 8 | Polish & Documentation | Not Started | All |

---

## Phase 1: PPDS.Auth Foundation

**Goal:** Create the shared auth package with profile storage and core credential providers.

### 1.1 Project Setup
- [ ] Create `src/PPDS.Auth/PPDS.Auth.csproj`
- [ ] Add to solution file
- [ ] Configure target framework (net8.0)
- [ ] Add package references (Azure.Identity, MSAL, Dataverse.Client)
- [ ] Create folder structure (Credentials/, Profiles/, Cloud/, Discovery/)

### 1.2 Cloud Configuration
- [ ] Create `CloudEnvironment.cs` enum (Public, UsGov, UsGovHigh, UsGovDod, China)
- [ ] Create `CloudEndpoints.cs` with authority URLs per cloud
- [ ] Create `CloudConfiguration.cs` for resolving endpoints

### 1.3 Profile Model
- [ ] Create `AuthProfile.cs` - profile data model
- [ ] Create `EnvironmentInfo.cs` - environment binding model
- [ ] Create `AuthMethod.cs` enum (DeviceCode, ClientSecret, Certificate, etc.)
- [ ] Create `ProfileCollection.cs` - collection with active tracking

### 1.4 Profile Storage
- [ ] Create `IProfileStore.cs` interface
- [ ] Create `ProfileStore.cs` - JSON file storage
- [ ] Create `ProfileEncryption.cs` - platform-specific encryption
  - [ ] Windows: DPAPI
  - [ ] macOS: Keychain (or fallback)
  - [ ] Linux: libsecret (or plaintext fallback)
- [ ] Create `ProfilePaths.cs` - platform-specific paths
- [ ] Unit tests for profile storage

### 1.5 Credential Provider Interface
- [ ] Create `ICredentialProvider.cs` interface
- [ ] Create `CredentialResult.cs` - result with ServiceClient
- [ ] Create `CredentialProviderFactory.cs` - creates provider from profile

### 1.6 Device Code Credential
- [ ] Create `DeviceCodeCredential.cs`
- [ ] Port token caching from existing `DeviceCodeTokenProvider`
- [ ] Integrate MSAL with shared token cache
- [ ] Handle device code callback for console output
- [ ] Unit tests

### 1.7 Client Secret Credential
- [ ] Create `ClientSecretCredential.cs`
- [ ] Handle secret decryption from profile
- [ ] Create ServiceClient with connection string
- [ ] Unit tests

### 1.8 ServiceClient Factory
- [ ] Create `IServiceClientFactory.cs` interface
- [ ] Create `ServiceClientFactory.cs`
- [ ] Resolve credential provider from profile
- [ ] Apply performance settings (thread pool, connection limits)
- [ ] Integration tests

**Phase 1 Exit Criteria:**
- [ ] Can create a profile with DeviceCode or ClientSecret auth
- [ ] Can load profile from storage
- [ ] Can create authenticated ServiceClient from profile
- [ ] Secrets are encrypted at rest

---

## Phase 2: CLI Restructure

**Goal:** Rename PPDS.Migration.Cli to PPDS.Cli and restructure command hierarchy.

### 2.1 Project Rename
- [ ] `git mv src/PPDS.Migration.Cli src/PPDS.Cli`
- [ ] Update solution file references
- [ ] Update csproj:
  - [ ] PackageId → PPDS.Cli
  - [ ] ToolCommandName → ppds
  - [ ] RootNamespace → PPDS.Cli
  - [ ] AssemblyName → ppds
- [ ] Add project reference to PPDS.Auth

### 2.2 Namespace Updates
- [ ] Update all .cs files: `PPDS.Migration.Cli` → `PPDS.Cli`
- [ ] Update using statements throughout

### 2.3 Command Folder Restructure
- [ ] Create `Commands/Auth/` folder
- [ ] Create `Commands/Env/` folder
- [ ] Create `Commands/Data/` folder
- [ ] Create `Commands/Schema/` folder
- [ ] Create `Commands/Users/` folder
- [ ] Move existing commands to appropriate folders:
  - [ ] ExportCommand.cs → Commands/Data/
  - [ ] ImportCommand.cs → Commands/Data/
  - [ ] MigrateCommand.cs → Commands/Data/CopyCommand.cs (rename)
  - [ ] AnalyzeCommand.cs → Commands/Data/
  - [ ] SchemaCommand.cs → Commands/Schema/ (split into GenerateCommand, ListCommand)
  - [ ] UsersCommand.cs → Commands/Users/ (split into GenerateCommand)

### 2.4 Program.cs Restructure
- [ ] Create command group classes (AuthCommandGroup, EnvCommandGroup, etc.)
- [ ] Update root command structure
- [ ] Remove old global auth options (--url, --auth)
- [ ] Add new global options (--verbose, --debug, --json)
- [ ] Wire up command groups

### 2.5 Infrastructure Updates
- [ ] Create `ConnectionResolver.cs` - resolve profile(s) to connections
- [ ] Update `ServiceFactory.cs` to use PPDS.Auth
- [ ] Create `OutputFormatter.cs` - console and JSON output
- [ ] Update `ExitCodes.cs` with new codes

### 2.6 Build Verification
- [ ] Solution builds successfully
- [ ] `dotnet pack` creates ppds tool
- [ ] `dotnet tool install --global --add-source ./nupkgs PPDS.Cli`
- [ ] `ppds --help` shows new command structure

**Phase 2 Exit Criteria:**
- [ ] CLI renamed and restructured
- [ ] `ppds --help` shows auth, env, data, schema, users groups
- [ ] Existing functionality still works (will wire up auth in Phase 3)

---

## Phase 3: Auth Commands

**Goal:** Implement all authentication management commands.

### 3.1 Auth Create Command
- [ ] Create `Commands/Auth/CreateCommand.cs`
- [ ] Implement all auth method flags:
  - [ ] --deviceCode
  - [ ] --applicationId + --clientSecret
  - [ ] --applicationId + --certificateDiskPath + --certificatePassword
  - [ ] --managedIdentity
  - [ ] --githubFederated (placeholder)
  - [ ] --azureDevOpsFederated (placeholder)
  - [ ] --username + --password (deprecated warning)
- [ ] Implement --name, --tenant, --cloud, --environment flags
- [ ] Validate credentials on create
- [ ] Auto-select first profile as active
- [ ] Store profile
- [ ] Console output matching PAC style

### 3.2 Auth List Command
- [ ] Create `Commands/Auth/ListCommand.cs`
- [ ] Table output with columns: #, Active, Name, Type, Cloud, Environment, Environment URL
- [ ] --json output option
- [ ] Handle unnamed profiles (blank name column)

### 3.3 Auth Select Command
- [ ] Create `Commands/Auth/SelectCommand.cs`
- [ ] Support --index and --name (mutually exclusive)
- [ ] Support positional name argument
- [ ] Update active profile in storage
- [ ] Error if profile not found

### 3.4 Auth Delete Command
- [ ] Create `Commands/Auth/DeleteCommand.cs`
- [ ] Support --index and --name (mutually exclusive)
- [ ] Remove profile from storage
- [ ] Handle active profile deletion (clear active or select next?)

### 3.5 Auth Update Command
- [ ] Create `Commands/Auth/UpdateCommand.cs`
- [ ] Require --index
- [ ] Support --name (rename)
- [ ] Support --environment (change target)
- [ ] Support --clientSecret (rotation)
- [ ] Support --certificateDiskPath + --certificatePassword (rotation)

### 3.6 Auth Name Command
- [ ] Create `Commands/Auth/NameCommand.cs`
- [ ] Require --index and --name
- [ ] Update profile name in storage
- [ ] Validate name (max 30 chars, valid characters)

### 3.7 Auth Clear Command
- [ ] Create `Commands/Auth/ClearCommand.cs`
- [ ] Delete all profiles
- [ ] Clear MSAL token cache
- [ ] No confirmation prompt
- [ ] Output: "Authentication profiles and token cache cleared."

### 3.8 Auth Who Command
- [ ] Create `Commands/Auth/WhoCommand.cs`
- [ ] Display current profile info:
  - [ ] Profile name (or index if unnamed)
  - [ ] Type (auth method)
  - [ ] Cloud
  - [ ] Tenant ID
  - [ ] User/Application ID
  - [ ] Token expiration
  - [ ] Environment (if selected)
- [ ] --json output option
- [ ] Error if no active profile

### 3.9 Integration Tests
- [ ] Test create/list/select/delete flow
- [ ] Test profile persistence across CLI invocations
- [ ] Test credential validation on create
- [ ] Test error cases (invalid creds, missing profile, etc.)

**Phase 3 Exit Criteria:**
- [ ] All 8 auth commands working
- [ ] Profiles persist between CLI runs
- [ ] `ppds auth create --deviceCode` authenticates and stores profile
- [ ] `ppds auth list` shows profiles matching PAC format
- [ ] `ppds auth who` shows current profile info

---

## Phase 4: Environment Discovery

**Goal:** Integrate Global Discovery Service for environment listing and selection.

### 4.1 GDS Client
- [ ] Create `Discovery/IGlobalDiscoveryService.cs` interface
- [ ] Create `Discovery/GlobalDiscoveryService.cs`
- [ ] Implement environment enumeration via GDS API
- [ ] Handle pagination if needed
- [ ] Create `Discovery/EnvironmentInfo.cs` with full details
- [ ] Cache results briefly (5 min?)
- [ ] Unit tests with mocked responses

### 4.2 Environment Resolution
- [ ] Create `Discovery/EnvironmentResolver.cs`
- [ ] Match by Environment ID (exact)
- [ ] Match by URL (exact or partial)
- [ ] Match by Display Name (exact or partial)
- [ ] Match by Unique Name (exact)
- [ ] Handle ambiguous matches (error with list)
- [ ] Unit tests

### 4.3 Env List Command
- [ ] Create `Commands/Env/ListCommand.cs`
- [ ] Query GDS for environments
- [ ] Table output: Active, Display Name, Environment ID, Environment URL, Type
- [ ] Mark active environment with *
- [ ] --json output option

### 4.4 Env Select Command
- [ ] Create `Commands/Env/SelectCommand.cs`
- [ ] Require --environment flag
- [ ] Resolve environment via GDS
- [ ] Validate connection to environment
- [ ] Store in current auth profile
- [ ] Output matching PAC style

### 4.5 Env Who Command
- [ ] Create `Commands/Env/WhoCommand.cs`
- [ ] Display current environment info:
  - [ ] Environment ID
  - [ ] Display Name
  - [ ] Unique Name
  - [ ] URL
  - [ ] Type (Sandbox/Production)
  - [ ] Organization ID
- [ ] Error if no environment selected
- [ ] --json output option

### 4.6 Auth Create Integration
- [ ] Update auth create to support --environment
- [ ] Resolve environment via GDS during creation
- [ ] Bind environment to profile

### 4.7 Integration Tests
- [ ] Test env list with real GDS
- [ ] Test env select with various inputs (name, partial, URL)
- [ ] Test profile environment binding persists

**Phase 4 Exit Criteria:**
- [ ] `ppds env list` shows all accessible environments
- [ ] `ppds env select --environment "Name"` binds to profile
- [ ] `ppds env who` shows current environment
- [ ] `ppds auth create --environment "Name"` binds at creation

---

## Phase 5: Additional Auth Methods

**Goal:** Implement remaining credential providers.

### 5.1 Certificate File Credential
- [ ] Create `CertificateFileCredential.cs`
- [ ] Load PFX/PEM from disk
- [ ] Handle certificate password
- [ ] Create ServiceClient with certificate
- [ ] Update auth create to support --certificateDiskPath
- [ ] Unit tests

### 5.2 Certificate Store Credential (Windows)
- [ ] Create `CertificateStoreCredential.cs`
- [ ] Load from Windows certificate store by thumbprint
- [ ] Support StoreName and StoreLocation options
- [ ] Update auth create to support --certificateThumbprint
- [ ] Unit tests (Windows only)

### 5.3 Managed Identity Credential
- [ ] Create `ManagedIdentityCredential.cs`
- [ ] Support system-assigned identity
- [ ] Support user-assigned identity (with --applicationId)
- [ ] Use Azure.Identity.ManagedIdentityCredential
- [ ] Update auth create for --managedIdentity
- [ ] Integration tests (Azure environment)

### 5.4 GitHub OIDC Credential
- [ ] Create `GitHubOidcCredential.cs`
- [ ] Detect GitHub Actions environment (ACTIONS_ID_TOKEN_REQUEST_*)
- [ ] Request OIDC token from GitHub
- [ ] Exchange for Azure AD token via client assertion
- [ ] Update auth create for --githubFederated
- [ ] Documentation for app registration setup
- [ ] Integration tests (GitHub Actions)

### 5.5 Azure DevOps OIDC Credential
- [ ] Create `AzureDevOpsOidcCredential.cs`
- [ ] Detect ADO environment (SYSTEM_OIDCREQUESTURI)
- [ ] Request OIDC token from ADO
- [ ] Exchange for Azure AD token
- [ ] Update auth create for --azureDevOpsFederated
- [ ] Documentation for service connection setup
- [ ] Integration tests (ADO pipeline)

### 5.6 Username/Password Credential (Deprecated)
- [ ] Create `UsernamePasswordCredential.cs`
- [ ] Show deprecation warning on use
- [ ] Implement with ROPC flow
- [ ] Update auth create for --username + --password
- [ ] Unit tests

**Phase 5 Exit Criteria:**
- [ ] All 8 auth methods working
- [ ] Each method has unit/integration tests
- [ ] Deprecation warning shown for username/password

---

## Phase 6: Data Command Integration

**Goal:** Wire up data commands to use new auth system.

### 6.1 Connection Resolver
- [ ] Create `Infrastructure/ConnectionResolver.cs`
- [ ] Resolve active profile if no --profile specified
- [ ] Resolve named profile(s) from --profile flag
- [ ] Validate all profiles have environment (or --environment provided)
- [ ] Apply --environment override to all profiles
- [ ] Return list of IConnectionSource for pool
- [ ] Error messages for common issues

### 6.2 Update Export Command
- [ ] Remove old --url, --auth flags
- [ ] Add --profile flag (optional, comma-separated names)
- [ ] Add --environment flag (optional, override)
- [ ] Use ConnectionResolver to get connections
- [ ] Update error handling for auth failures

### 6.3 Update Import Command
- [ ] Remove old --url, --auth flags
- [ ] Add --profile flag
- [ ] Add --environment flag
- [ ] Use ConnectionResolver
- [ ] Ensure pooling still works with new auth

### 6.4 Update Copy Command (was Migrate)
- [ ] Rename MigrateCommand to CopyCommand
- [ ] Add --source-env and --target-env flags
- [ ] Add --profile, --source-profile, --target-profile
- [ ] Handle two different environments
- [ ] Update service creation for both connections

### 6.5 Update Analyze Command
- [ ] Verify no auth needed (offline analysis)
- [ ] No changes needed (just namespace updates)

### 6.6 Update Schema Commands
- [ ] Update GenerateCommand with new auth
- [ ] Update ListCommand with new auth
- [ ] Add --profile and --environment flags

### 6.7 Update Users Command
- [ ] Update GenerateCommand with new auth
- [ ] Add source/target profile and environment flags

### 6.8 Integration Tests
- [ ] Test export with device code profile
- [ ] Test import with client secret profile
- [ ] Test copy with two environments
- [ ] Test schema list with profile
- [ ] Test error cases (no env, wrong env, etc.)

**Phase 6 Exit Criteria:**
- [ ] All data commands use new profile-based auth
- [ ] Active profile works without --profile flag
- [ ] --environment overrides profile environment
- [ ] Error messages guide user to fix issues

---

## Phase 7: Pooling Support

**Goal:** Enable multiple profiles for high-throughput operations.

### 7.1 Multi-Profile Parsing
- [ ] Update --profile to accept comma-separated names
- [ ] Parse into list of profile names
- [ ] Resolve each to profile object

### 7.2 Environment Validation
- [ ] When multiple profiles, validate same environment
- [ ] If --environment provided, override all
- [ ] If profiles have different environments, error with details
- [ ] If any profile has no environment and no --environment, error

### 7.3 Pool Integration
- [ ] Create IConnectionSource from each profile
- [ ] Pass array to DataverseConnectionPool
- [ ] Verify throttle tracking works per-source
- [ ] Verify adaptive rate control works

### 7.4 Import Command Pooling
- [ ] Test with multiple --profile values
- [ ] Verify parallel execution
- [ ] Verify throughput improvement
- [ ] Test throttle recovery across pool

### 7.5 Export Command Pooling
- [ ] Test with multiple --profile values
- [ ] Verify works for export (read operations)

### 7.6 Documentation
- [ ] Update CLI help text for pooling
- [ ] Add pooling examples to README
- [ ] Document Application User setup

**Phase 7 Exit Criteria:**
- [ ] `ppds data import --profile a,b,c` uses all three connections
- [ ] Throughput scales with number of profiles
- [ ] Validation prevents cross-environment pooling

---

## Phase 8: Polish & Documentation

**Goal:** Production-ready quality and documentation.

### 8.1 Error Handling
- [ ] Consistent error message format
- [ ] Helpful suggestions in error messages
- [ ] Proper exit codes for all scenarios
- [ ] No stack traces in release mode (unless --debug)

### 8.2 Output Formatting
- [ ] Consistent table formatting
- [ ] Proper JSON output for all commands
- [ ] Progress indicators for long operations
- [ ] Color output (with --no-color option?)

### 8.3 Help Text
- [ ] Review all command help text
- [ ] Add examples to command help
- [ ] Ensure flag descriptions are clear

### 8.4 Testing
- [ ] Unit test coverage > 80%
- [ ] Integration tests for happy paths
- [ ] Error case tests
- [ ] Cross-platform testing (Windows, Linux, macOS)

### 8.5 Documentation
- [ ] Update PPDS.Cli README
- [ ] Update PPDS.Auth README
- [ ] Migration guide from ppds-migrate
- [ ] Pooling setup guide
- [ ] CI/CD examples (GitHub Actions, ADO)

### 8.6 CHANGELOG Updates
- [ ] PPDS.Cli CHANGELOG
- [ ] PPDS.Auth CHANGELOG (new package)
- [ ] Note breaking changes from ppds-migrate

### 8.7 Final Cleanup
- [ ] Remove deprecated code
- [ ] Remove old PPDS.Migration.Cli references
- [ ] Update CLAUDE.md with new commands
- [ ] Review and update solution README

**Phase 8 Exit Criteria:**
- [ ] All tests passing
- [ ] Documentation complete
- [ ] Ready for v1.0.0 release

---

## Notes & Decisions Log

| Date | Decision | Rationale |
|------|----------|-----------|
| 2025-01-27 | Profile flag accepts names only, not indexes | Stability for pooling; indexes can shift |
| 2025-01-27 | Rename PPDS.Migration.Cli in place | Preserve git history |
| 2025-01-27 | Use `ppds env` not `ppds environment` | Match PAC, shorter |
| 2025-01-27 | `data copy` instead of `data migrate` | Avoid `ppds migrate migrate` confusion |
| 2025-01-27 | No confirmation for `auth clear` | CI-friendly, PAC parity |
| 2025-01-27 | Environment not auto-selected | Explicit is safer, PAC parity |

---

## Blockers & Issues

| Issue | Status | Resolution |
|-------|--------|------------|
| None yet | - | - |

---

## Progress Summary

| Phase | Tasks | Complete | Remaining |
|-------|-------|----------|-----------|
| 1 | 32 | 0 | 32 |
| 2 | 24 | 0 | 24 |
| 3 | 36 | 0 | 36 |
| 4 | 24 | 0 | 24 |
| 5 | 24 | 0 | 24 |
| 6 | 28 | 0 | 28 |
| 7 | 20 | 0 | 20 |
| 8 | 28 | 0 | 28 |
| **Total** | **216** | **0** | **216** |

---

## Quick Reference

```bash
# Build
dotnet build

# Test
dotnet test

# Pack and test locally
dotnet pack -c Release -o ./nupkgs
dotnet tool install --global --add-source ./nupkgs PPDS.Cli

# Uninstall for re-testing
dotnet tool uninstall --global PPDS.Cli
```
