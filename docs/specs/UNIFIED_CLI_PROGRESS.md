# PPDS Unified CLI - Implementation Progress

**Spec:** [UNIFIED_CLI_SPEC.md](UNIFIED_CLI_SPEC.md)
**ADR:** [ADR-0008](../adr/0008_UNIFIED_CLI_AND_AUTH.md)
**Branch:** `feature/unified-cli-auth`
**Started:** 2025-01-27

---

## Phase Overview

| Phase | Description | Status | Dependencies |
|-------|-------------|--------|--------------|
| 1 | PPDS.Auth Foundation | **Complete** | - |
| 2 | CLI Restructure + Auth Commands | **Complete** | Phase 1 |
| 3 | Environment Discovery | Not Started | Phase 1, 2 |
| 4 | Additional Auth Methods | Not Started | Phase 2 |
| 5 | Data Command Integration | Not Started | Phase 2, 3 |
| 6 | Pooling Support | Not Started | Phase 5 |
| 7 | Polish & Documentation | Not Started | All |

---

## Phase 1: PPDS.Auth Foundation - COMPLETE

**Goal:** Create the shared auth package with profile storage and core credential providers.

### Completed Items
- [x] Create `src/PPDS.Auth/PPDS.Auth.csproj`
- [x] Add to solution file
- [x] Configure target framework (net8.0)
- [x] Add package references (Azure.Identity, MSAL, Dataverse.Client)
- [x] Create folder structure (Credentials/, Profiles/, Cloud/)
- [x] Create `CloudEnvironment.cs` enum (Public, UsGov, UsGovHigh, UsGovDod, China)
- [x] Create `CloudEndpoints.cs` with authority URLs per cloud
- [x] Create `AuthProfile.cs` - profile data model
- [x] Create `EnvironmentInfo.cs` - environment binding model
- [x] Create `AuthMethod.cs` enum (DeviceCode, ClientSecret, Certificate, etc.)
- [x] Create `ProfileCollection.cs` - collection with active tracking
- [x] Create `ProfileStore.cs` - JSON file storage
- [x] Create `ProfileEncryption.cs` - DPAPI encryption for Windows
- [x] Create `ProfilePaths.cs` - platform-specific paths
- [x] Create `ICredentialProvider.cs` interface
- [x] Create `CredentialProviderFactory.cs` - creates provider from profile
- [x] Create `DeviceCodeCredentialProvider.cs` - interactive login
- [x] Create `ClientSecretCredentialProvider.cs` - app secret auth
- [x] Create `ServiceClientFactory.cs` - factory with performance settings
- [x] Create `AuthenticationException.cs` - custom exception

**Files Created:** 17 files in PPDS.Auth package

---

## Phase 2: CLI Restructure + Auth Commands - COMPLETE

**Goal:** Rename PPDS.Migration.Cli to PPDS.Cli, restructure command hierarchy, implement auth commands.

### 2.1 Project Rename - COMPLETE
- [x] `git mv src/PPDS.Migration.Cli src/PPDS.Cli`
- [x] Rename csproj: `PPDS.Migration.Cli.csproj` → `PPDS.Cli.csproj`
- [x] Update solution file references
- [x] Update csproj:
  - [x] PackageId → PPDS.Cli
  - [x] ToolCommandName → ppds
  - [x] RootNamespace → PPDS.Cli
  - [x] AssemblyName → ppds
  - [x] MinVerTagPrefix → Cli-v
- [x] Add project reference to PPDS.Auth

### 2.2 Namespace Updates - COMPLETE
- [x] Update all .cs files: `PPDS.Migration.Cli` → `PPDS.Cli`
- [x] Update using statements throughout

### 2.3 Command Folder Restructure - COMPLETE
- [x] Create `Commands/Auth/` folder
- [x] Create `Commands/Env/` folder (placeholder)
- [x] Create `Commands/Data/` folder
- [x] Move existing commands to Data folder:
  - [x] ExportCommand.cs → Commands/Data/
  - [x] ImportCommand.cs → Commands/Data/
  - [x] MigrateCommand.cs → Commands/Data/CopyCommand.cs (renamed)
  - [x] AnalyzeCommand.cs → Commands/Data/
- [x] Create DataCommandGroup.cs - wraps data subcommands
- [x] Update namespaces to PPDS.Cli.Commands.Data

### 2.4 Program.cs Restructure - COMPLETE
- [x] Create AuthCommandGroup with all commands
- [x] Add command groups to root command
- [x] Keep existing global auth options (--url, --auth) for compatibility
- [x] Wire up auth and data command groups

### 2.5 Test Updates - COMPLETE
- [x] Rename test project folder and csproj
- [x] Update test namespaces
- [x] Rename MigrateCommandTests.cs → CopyCommandTests.cs
- [x] Update test assertions for new command names
- [x] All 792 tests passing

### 2.6 Auth Commands - COMPLETE
- [x] `ppds auth create` - Create new profile (interactive login)
- [x] `ppds auth list` - List all profiles (text and JSON)
- [x] `ppds auth select` - Select active profile
- [x] `ppds auth delete` - Delete a profile (with confirmation)
- [x] `ppds auth update` - Re-authenticate existing profile
- [x] `ppds auth name` - Rename a profile
- [x] `ppds auth clear` - Delete all profiles and credentials
- [x] `ppds auth who` - Show current active profile

**Files Modified/Created:**
- Modified: 20+ files (namespace updates)
- Created: Commands/Auth/AuthCommandGroup.cs
- Created: Commands/Data/DataCommandGroup.cs
- Renamed: MigrateCommand.cs → CopyCommand.cs

---

## Phase 3: Environment Discovery - NOT STARTED

**Goal:** Integrate Global Discovery Service for environment listing and selection.

### 3.1 GDS Client
- [ ] Create `Discovery/IGlobalDiscoveryService.cs` interface
- [ ] Create `Discovery/GlobalDiscoveryService.cs`
- [ ] Implement environment enumeration via GDS API
- [ ] Handle pagination if needed
- [ ] Cache results briefly (5 min?)

### 3.2 Environment Resolution
- [ ] Create `Discovery/EnvironmentResolver.cs`
- [ ] Match by Environment ID, URL, Display Name, Unique Name
- [ ] Handle ambiguous matches

### 3.3 Env Commands
- [ ] Create `Commands/Env/EnvCommandGroup.cs`
- [ ] Create `ppds env list` - List accessible environments
- [ ] Create `ppds env select` - Bind environment to profile
- [ ] Create `ppds env who` - Show current environment

### 3.4 Auth Create Integration
- [ ] Update auth create to support --environment
- [ ] Resolve environment via GDS during creation
- [ ] Bind environment to profile

---

## Phase 4: Additional Auth Methods - NOT STARTED

**Goal:** Implement remaining credential providers.

- [ ] Certificate File Credential (--certificateDiskPath)
- [ ] Certificate Store Credential (--certificateThumbprint, Windows only)
- [ ] Managed Identity Credential (--managedIdentity)
- [ ] GitHub OIDC Credential (--githubFederated)
- [ ] Azure DevOps OIDC Credential (--azureDevOpsFederated)
- [ ] Username/Password Credential (deprecated warning)

---

## Phase 5: Data Command Integration - NOT STARTED

**Goal:** Wire up data commands to use new profile-based auth.

- [ ] Create ConnectionResolver for profile → connection mapping
- [ ] Update Export/Import/Copy commands with --profile flag
- [ ] Support --environment override
- [ ] Ensure pooling works with new auth

---

## Phase 6: Pooling Support - NOT STARTED

**Goal:** Enable multiple profiles for high-throughput operations.

- [ ] Multi-profile parsing (--profile a,b,c)
- [ ] Environment validation for multi-profile
- [ ] Pool integration with IConnectionSource
- [ ] Documentation

---

## Phase 7: Polish & Documentation - NOT STARTED

**Goal:** Production-ready quality and documentation.

- [ ] Error handling consistency
- [ ] Output formatting
- [ ] Help text improvements
- [ ] Unit test coverage > 80%
- [ ] Documentation updates
- [ ] CHANGELOG updates

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
| 2025-12-27 | Combined CLI restructure and auth commands in Phase 2 | Reduced context switches |

---

## Blockers & Issues

| Issue | Status | Resolution |
|-------|--------|------------|
| None | - | - |

---

## Progress Summary

| Phase | Description | Status |
|-------|-------------|--------|
| 1 | PPDS.Auth Foundation | **Complete** |
| 2 | CLI Restructure + Auth Commands | **Complete** |
| 3 | Environment Discovery | Not Started |
| 4 | Additional Auth Methods | Not Started |
| 5 | Data Command Integration | Not Started |
| 6 | Pooling Support | Not Started |
| 7 | Polish & Documentation | Not Started |

**Overall Progress:** Phase 2 of 7 complete (~30%)

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

# Test auth commands
ppds auth create --name dev --cloud Public
ppds auth list
ppds auth who
ppds auth select dev
ppds auth name dev prod
ppds auth delete dev --force
ppds auth clear --force
```
