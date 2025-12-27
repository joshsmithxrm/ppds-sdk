# PPDS Unified CLI Specification

**Version:** 1.0
**Status:** Draft
**Created:** 2025-01-27
**Authors:** Claude + Josh

---

## Overview

This specification defines the unified PPDS CLI (`ppds`) that consolidates all command-line functionality into a single tool with shared authentication infrastructure. The CLI provides PAC CLI-compatible authentication and environment management while adding support for high-throughput data migrations via connection pooling.

### Goals

1. **PAC CLI Parity** - Match PAC CLI authentication UX for familiar experience
2. **Unified Tool** - Single CLI for all PPDS operations (auth, data, schema, users)
3. **Shared Auth** - Common authentication infrastructure usable by future CLIs
4. **High-Throughput Pooling** - Support multiple auth profiles for parallel operations
5. **Environment Discovery** - Integrate with Global Discovery Service

### Non-Goals

1. Full PAC CLI feature parity (we focus on data/migration scenarios)
2. GUI or interactive menus
3. Plugin/extension system (defer to future version)

---

## Package Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                         PPDS.Cli                                 │
│  Unified CLI tool (dotnet tool)                                 │
│  - Command routing and parsing                                   │
│  - Output formatting (console, JSON)                            │
│  - References: PPDS.Auth, PPDS.Migration, PPDS.Dataverse        │
└─────────────────────────────────────────────────────────────────┘
                              │
        ┌─────────────────────┼─────────────────────┐
        ▼                     ▼                     ▼
┌───────────────┐    ┌────────────────┐    ┌────────────────┐
│   PPDS.Auth   │    │ PPDS.Migration │    │ PPDS.Dataverse │
│               │    │                │    │                │
│ - Profiles    │    │ - Export       │    │ - Connection   │
│ - Credentials │    │ - Import       │    │   Pool         │
│ - GDS Client  │    │ - Schema       │    │ - Bulk Ops     │
│ - Token Cache │    │ - User Mapping │    │ - Resilience   │
└───────────────┘    └────────────────┘    └────────────────┘
```

### PPDS.Auth (New Package)

Shared authentication infrastructure:
- Profile storage and encryption
- Credential providers for all auth methods
- Global Discovery Service client
- Token caching (MSAL integration)
- ServiceClient factory

### PPDS.Cli (Replaces PPDS.Migration.Cli)

Unified command-line tool:
- Tool name: `ppds`
- NuGet package: `PPDS.Cli`
- Target framework: net8.0

---

## Command Structure

```
ppds
├── auth
│   ├── create        Create authentication profile
│   ├── list          List all profiles
│   ├── select        Set active profile
│   ├── delete        Delete a profile
│   ├── update        Update profile settings
│   ├── name          Name/rename a profile
│   ├── clear         Clear all profiles and token cache
│   └── who           Show current auth info
│
├── env
│   ├── list          List environments from GDS
│   ├── select        Select environment for current profile
│   └── who           Show current environment info
│
├── data
│   ├── export        Export data from Dataverse
│   ├── import        Import data to Dataverse
│   ├── copy          Export + import in one operation
│   └── analyze       Analyze schema/data files
│
├── schema
│   ├── generate      Generate migration schema
│   └── list          List available entities
│
└── users
    └── generate      Generate user mapping file
```

---

## Authentication Model

### Two-Layer Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│  Auth Profile (WHO - stored locally)                            │
│  ├── Credentials (user, app+secret, certificate, etc.)         │
│  ├── Cloud (Public, UsGov, UsGovHigh, UsGovDod, China)         │
│  ├── Tenant ID                                                  │
│  └── Selected Environment (optional)                            │
│      ├── Environment ID                                         │
│      ├── Environment URL                                        │
│      ├── Display Name                                           │
│      └── Unique Name                                            │
└─────────────────────────────────────────────────────────────────┘
```

### Key Behaviors

1. **Profiles can be named or unnamed** - Unnamed profiles are referenced by index
2. **First profile is auto-selected as active** - Subsequent creates do not change active
3. **Environment is optional** - Profiles can be "universal" (no environment bound)
4. **`env select` binds environment to current profile** - Stored as part of profile
5. **Commands can override with `--environment`** - Takes precedence over profile env

### Auth Profile Storage

**Location:**
- Windows: `%LOCALAPPDATA%\PPDS\profiles.json`
- macOS/Linux: `~/.ppds/profiles.json`

**Encryption:**
- Windows: DPAPI (DataProtectionScope.CurrentUser)
- macOS: Keychain
- Linux: libsecret/Secret Service (with plaintext fallback)

**Schema:**
```json
{
  "version": 1,
  "activeIndex": 1,
  "profiles": [
    {
      "index": 1,
      "name": "dev",
      "authMethod": "DeviceCode",
      "cloud": "Public",
      "tenantId": "34502e2f-89bb-4550-8a28-1d734e433e88",
      "environment": {
        "id": "8064eccb-3a2d-ed88-9384-35a884d1fb0c",
        "url": "https://orgcabef92d.crm.dynamics.com/",
        "displayName": "PPDS Demo - Dev",
        "uniqueName": "unq3a504f4385d7f01195c7000d3a5cc"
      },
      "createdAt": "2025-01-27T10:30:00Z",
      "lastUsedAt": "2025-01-27T14:22:00Z"
    },
    {
      "index": 2,
      "name": null,
      "authMethod": "ClientSecret",
      "cloud": "Public",
      "tenantId": "34502e2f-89bb-4550-8a28-1d734e433e88",
      "applicationId": "abc123-def4-5678-90ab-cdef12345678",
      "clientSecret": "ENCRYPTED:AQAAANCMnd8BFdERjHoAwE...",
      "environment": null,
      "createdAt": "2025-01-27T11:00:00Z"
    }
  ]
}
```

### Token Cache

**Location:**
- Windows: `%LOCALAPPDATA%\PPDS\msal_token_cache.bin`
- macOS/Linux: `~/.ppds/msal_token_cache.bin`

**Strategy:** Shared MSAL cache across all profiles. MSAL manages account isolation internally.

### Profile Reference Behavior

Different commands accept different ways to reference profiles:

| Command Context | `--name` | `--index` | `--profile` | Positional |
|-----------------|----------|-----------|-------------|------------|
| `auth select` | ✅ | ✅ | - | ✅ (name) |
| `auth delete` | ✅ | ✅ | - | - |
| `auth update` | ✅ (new name) | ✅ (required) | - | - |
| `auth name` | ✅ (required) | ✅ (required) | - | - |
| `data export` | - | - | ✅ (names only) | - |
| `data import` | - | - | ✅ (names only) | - |
| `data copy` | - | - | ✅ (names only) | - |
| `schema *` | - | - | ✅ (names only) | - |
| `users *` | - | - | ✅ (names only) | - |

**Key Rules:**
1. **Auth commands** - Accept `--index` or `--name` (one required, not both)
2. **Data/Schema/Users commands** - Accept `--profile` with **names only** (not indexes)
3. **Unnamed profiles** - Must be named before use with `--profile` (use `ppds auth name`)
4. **Active profile** - Used automatically when `--profile` is omitted

**Rationale for names-only on data commands:**
- Indexes can shift when profiles are deleted (profile 2 becomes profile 1)
- Pooling scenarios need stable, self-documenting identifiers
- Scripts using `--profile app1,app2,app3` are readable and reliable

---

## Authentication Methods

| Method | CLI Flags | Description |
|--------|-----------|-------------|
| Device Code | `--deviceCode` | Interactive browser auth (default for users) |
| Client Secret | `--applicationId` + `--clientSecret` | Service principal with secret |
| Certificate File | `--applicationId` + `--certificateDiskPath` | Certificate from PFX/PEM file |
| Certificate Store | `--applicationId` + `--certificateThumbprint` | Certificate from Windows store |
| Managed Identity | `--managedIdentity` | Azure Managed Identity |
| GitHub OIDC | `--githubFederated` | GitHub Actions workload identity |
| Azure DevOps OIDC | `--azureDevOpsFederated` | Azure DevOps workload identity |
| Username/Password | `--username` + `--password` | Legacy (deprecated, with warning) |

### Cloud Environments

| Value | Authority URL |
|-------|---------------|
| `Public` | `https://login.microsoftonline.com` |
| `UsGov` | `https://login.microsoftonline.us` |
| `UsGovHigh` | `https://login.microsoftonline.us` |
| `UsGovDod` | `https://login.microsoftonline.us` |
| `China` | `https://login.chinacloudapi.cn` |

---

## Command Reference

### `ppds auth create`

Create a new authentication profile.

```
Usage: ppds auth create [options]

Options:
  -n, --name <name>              Profile name (max 30 chars, optional)

  Authentication (pick one):
  --deviceCode, -dc              Interactive device code flow (default)
  --applicationId, -id <id>      Application (client) ID
  --clientSecret, -cs <secret>   Client secret (with --applicationId)
  --certificateDiskPath, -cdp    Certificate file path (with --applicationId)
  --certificatePassword, -cp     Certificate password
  --certificateThumbprint        Certificate thumbprint from store
  --managedIdentity, -mi         Azure Managed Identity
  --githubFederated, -ghf        GitHub Actions OIDC (preview)
  --azureDevOpsFederated, -adof  Azure DevOps OIDC (preview)
  --username, -un <user>         Username (deprecated)
  --password, -p <pass>          Password (deprecated)

  --tenant, -t <id>              Tenant ID (required for app auth)
  --cloud, -ci <cloud>           Cloud: Public, UsGov, UsGovHigh, UsGovDod, China
  --environment, -env <name>     Default environment (ID, URL, name, or partial)
```

**Examples:**
```bash
# Interactive auth (universal, no environment)
ppds auth create --deviceCode

# Interactive with name and environment
ppds auth create --name dev --deviceCode --environment "PPDS Demo - Dev"

# Service principal
ppds auth create --name prod-ci \
  --applicationId abc123-... \
  --clientSecret $SECRET \
  --tenant 34502e2f-... \
  --environment "PPDS Demo - Prod"

# Certificate auth
ppds auth create --name prod-cert \
  --applicationId abc123-... \
  --certificateDiskPath /path/to/cert.pfx \
  --certificatePassword $CERT_PASS \
  --tenant 34502e2f-...

# GitHub Actions OIDC
ppds auth create --name ci \
  --applicationId abc123-... \
  --githubFederated \
  --tenant 34502e2f-...

# Managed Identity (Azure-hosted)
ppds auth create --name azure-mi --managedIdentity

# Government cloud
ppds auth create --name gov --deviceCode --cloud UsGov
```

**Behavior:**
- Validates credentials by attempting connection
- First profile is auto-selected as active
- With `--environment`: resolves via GDS and binds to profile
- Without `--environment`: profile is "universal"

---

### `ppds auth list`

List all authentication profiles.

```
Usage: ppds auth list [--json]
```

**Output:**
```
  #  Active  Name      Type          Cloud   Environment      Environment URL
  1  *       dev       DeviceCode    Public  PPDS Demo - Dev  https://orgcabef92d.crm.dynamics.com/
  2          prod-ci   ClientSecret  Public  PPDS Demo - Prod https://org45808e40.crm.dynamics.com/
  3                    ClientSecret  Public
```

---

### `ppds auth select`

Set the active authentication profile.

```
Usage: ppds auth select [--index <n>] [--name <name>]

Options:
  -i, --index <n>     Profile index
  -n, --name <name>   Profile name

Note: Provide either --index or --name, not both.
      Positional argument is treated as --name.
```

**Examples:**
```bash
ppds auth select dev           # By name (positional)
ppds auth select --name dev    # By name (explicit)
ppds auth select --index 2     # By index
```

---

### `ppds auth delete`

Delete an authentication profile.

```
Usage: ppds auth delete [--index <n>] [--name <name>]
```

**Behavior:** No confirmation prompt. Use with care.

---

### `ppds auth update`

Update an existing authentication profile.

```
Usage: ppds auth update --index <n> [options]

Options:
  -i, --index <n>              Profile index (required)
  -n, --name <name>            New name
  --environment, -env <name>   New environment
  --clientSecret <secret>      New client secret (rotation)
  --certificateDiskPath        New certificate path
  --certificatePassword        New certificate password
```

**Examples:**
```bash
# Change environment
ppds auth update --index 1 --environment "PPDS Demo - Prod"

# Rotate secret
ppds auth update --index 2 --clientSecret $NEW_SECRET

# Rename
ppds auth update --index 1 --name "dev-new-name"
```

---

### `ppds auth name`

Name or rename an authentication profile.

```
Usage: ppds auth name --index <n> --name <name>

Options:
  -i, --index <n>     Profile index (required)
  -n, --name <name>   New name (required, max 30 chars)
```

---

### `ppds auth clear`

Delete all authentication profiles and clear token cache.

```
Usage: ppds auth clear
```

**Behavior:** No confirmation prompt. Executes immediately (CI-friendly).

**Output:**
```
Authentication profiles and token cache cleared.
```

---

### `ppds auth who`

Display information about the current authentication profile.

```
Usage: ppds auth who [--json]
```

**Output:**
```
Profile:              dev
Type:                 DeviceCode
Cloud:                Public
Tenant ID:            34502e2f-89bb-4550-8a28-1d734e433e88
User:                 josh@contoso.com
Token Expires:        12/27/2025 10:52:34 PM +00:00
Environment:          PPDS Demo - Dev
Environment ID:       8064eccb-3a2d-ed88-9384-35a884d1fb0c
Environment URL:      https://orgcabef92d.crm.dynamics.com/
```

---

### `ppds env list`

List accessible Dataverse environments from Global Discovery Service.

```
Usage: ppds env list [--json]
```

**Output:**
```
Connected as josh@contoso.com

Active  Display Name      Environment ID                        Environment URL                        Type
        PPDS Demo - Dev   8064eccb-3a2d-ed88-9384-35a884d1fb0c  https://orgcabef92d.crm.dynamics.com/  Sandbox
*       PPDS Demo - QA    ae2847d5-a7ec-e070-aa21-8a0f75da6d19  https://orge821e2a2.crm.dynamics.com/  Sandbox
        PPDS Demo - Prod  c6b51f1e-986a-e184-b075-87d04112ba16  https://org45808e40.crm.dynamics.com/  Production
```

---

### `ppds env select`

Select default environment for the current authentication profile.

```
Usage: ppds env select --environment <env>

Options:
  -env, --environment <env>   Environment (ID, URL, unique name, or partial display name)
```

**Examples:**
```bash
ppds env select --environment "PPDS Demo - Dev"
ppds env select --environment "Demo - Dev"              # Partial match
ppds env select --environment "8064eccb-3a2d-ed88..."   # By ID
ppds env select --environment "https://orgcabef92d..."  # By URL
```

**Behavior:**
1. Queries GDS to find matching environment
2. Validates connection to environment
3. Stores environment in current auth profile

**Output:**
```
Connected as josh@contoso.com
Looking for environment 'PPDS Demo - Dev'...
Validating connection...
Connected to... PPDS Demo - Dev
Selected environment 'PPDS Demo - Dev' (https://orgcabef92d.crm.dynamics.com/) for current authentication profile.
```

---

### `ppds env who`

Display information about the currently selected environment.

```
Usage: ppds env who [--json]
```

**Output:**
```
Connected as josh@contoso.com
Connected to... PPDS Demo - Dev

Environment Information
  Environment ID:     8064eccb-3a2d-ed88-9384-35a884d1fb0c
  Display Name:       PPDS Demo - Dev
  Unique Name:        unq3a504f4385d7f01195c7000d3a5cc
  URL:                https://orgcabef92d.crm.dynamics.com/
  Type:               Sandbox
  Organization ID:    3a504f43-85d7-f011-95c7-000d3a5cc636
```

**Error (no environment selected):**
```
Error: No environment selected for the current auth profile.

To fix this, either:
  1. Select an environment: ppds env select --environment "Name"
  2. Specify on command: --environment "Name"
```

---

### `ppds data export`

Export data from Dataverse.

```
Usage: ppds data export [options]

Required:
  -s, --schema <path>           Path to schema.xml file

Output:
  -o, --output <path>           Output file path (.zip)

Connection:
  -p, --profile <name(s)>       Profile(s) to use (comma-separated for pooling)
  -env, --environment <env>     Override environment (ID, URL, or name)

Options:
  --json                        JSON output format
  -v, --verbose                 Verbose output
  --debug                       Debug output
```

**Examples:**
```bash
# Using active profile with bound environment
ppds data export --schema schema.xml --output data.zip

# Explicit profile
ppds data export --profile dev --schema schema.xml --output data.zip

# Override environment
ppds data export --profile dev --environment "PPDS Demo - QA" --schema schema.xml --output data.zip

# Pooling (multiple profiles)
ppds data export --profile app1,app2,app3 --schema schema.xml --output data.zip
```

---

### `ppds data import`

Import data to Dataverse.

```
Usage: ppds data import [options]

Required:
  -d, --data <path>             Data file to import (.zip)

Connection:
  -p, --profile <name(s)>       Profile(s) to use (comma-separated for pooling)
  -env, --environment <env>     Override environment

Options:
  --mode <mode>                 Import mode: Insert, Update, Upsert (default: Upsert)
  --user-mapping <path>         User mapping file
  --bypass-plugins              Bypass custom plugin execution
  --bypass-flows                Bypass Power Automate flow triggers
  --continue-on-error           Continue after failures
  --json                        JSON output format
  -v, --verbose                 Verbose output
  --debug                       Debug output
```

**Pooling Example:**
```bash
# 3 app registrations for 3x throughput
ppds data import \
  --profile prod-app1,prod-app2,prod-app3 \
  --environment "PPDS Demo - Prod" \
  --data large-dataset.zip \
  --mode Upsert
```

---

### `ppds data copy`

Export from source and import to target in one operation.

```
Usage: ppds data copy [options]

Required:
  -s, --schema <path>           Path to schema.xml file
  --source-env <env>            Source environment
  --target-env <env>            Target environment

Connection:
  -p, --profile <name(s)>       Profile(s) for both source and target
  --source-profile <name(s)>    Profile(s) for source only
  --target-profile <name(s)>    Profile(s) for target only

Options:
  --temp-dir <path>             Temp directory for intermediate file
  --bypass-plugins              Bypass plugins on target
  --bypass-flows                Bypass flows on target
  --json                        JSON output format
  -v, --verbose                 Verbose output
  --debug                       Debug output
```

**Example:**
```bash
ppds data copy \
  --schema schema.xml \
  --source-env "PPDS Demo - Dev" \
  --target-env "PPDS Demo - QA" \
  --profile dev-user
```

---

### `ppds data analyze`

Analyze schema or data files (offline, no connection required).

```
Usage: ppds data analyze [options]

Input (one required):
  -s, --schema <path>           Analyze schema file
  -d, --data <path>             Analyze data file

Options:
  --json                        JSON output format
  -v, --verbose                 Verbose output
```

---

### `ppds schema generate`

Generate migration schema from Dataverse metadata.

```
Usage: ppds schema generate [options]

Required:
  -e, --entities <names>        Entity logical names (comma-separated)
  -o, --output <path>           Output schema file path

Connection:
  -p, --profile <name>          Profile to use
  -env, --environment <env>     Override environment

Options:
  --include-system-fields       Include system fields
  --include-relationships       Include relationships (default: true)
  --disable-plugins             Set disableplugins=true on all entities
  -a, --include-attributes      Only include these attributes (whitelist)
  --exclude-attributes          Exclude these attributes (blacklist)
  --exclude-patterns            Exclude attributes matching patterns
  --json                        JSON output format
  -v, --verbose                 Verbose output
  --debug                       Debug output
```

---

### `ppds schema list`

List available entities in Dataverse.

```
Usage: ppds schema list [options]

Connection:
  -p, --profile <name>          Profile to use
  -env, --environment <env>     Override environment

Options:
  -f, --filter <pattern>        Filter by name pattern (e.g., 'account*')
  --custom-only                 Show only custom entities
  --json                        JSON output format
```

---

### `ppds users generate`

Generate user mapping file for cross-environment migration.

```
Usage: ppds users generate [options]

Required:
  --source-env <env>            Source environment
  --target-env <env>            Target environment
  -o, --output <path>           Output user mapping file

Connection:
  -p, --profile <name>          Profile for both environments
  --source-profile <name>       Profile for source only
  --target-profile <name>       Profile for target only

Options:
  --analyze                     Analyze only, don't generate file
  --json                        JSON output format
  -v, --verbose                 Verbose output
  --debug                       Debug output
```

---

## Connection Pooling

### Overview

For high-throughput migrations, multiple auth profiles (each with separate Application User credentials) can be pooled to multiply API quota:

- Each Application User gets 6,000 requests per 5-minute window
- 3 Application Users = 18,000 requests per 5-minute window
- Pool distributes load and handles throttle recovery automatically

### Usage

```bash
# Create profiles for each Application User
ppds auth create --name app1 --applicationId $APP1_ID --clientSecret $APP1_SECRET --tenant $TENANT
ppds auth create --name app2 --applicationId $APP2_ID --clientSecret $APP2_SECRET --tenant $TENANT
ppds auth create --name app3 --applicationId $APP3_ID --clientSecret $APP3_SECRET --tenant $TENANT

# Use pooled for import
ppds data import \
  --profile app1,app2,app3 \
  --environment "Prod" \
  --data large-dataset.zip
```

### Validation Rules

1. **All profiles must resolve to the same environment**
2. **`--environment` on command overrides profile environments**
3. **If any profile has no environment and no `--environment` flag, error**

**Error Examples:**

```
Error: Profiles target different environments:
  - app1: PPDS Demo - Prod (https://org45808e40.crm.dynamics.com/)
  - app2: PPDS Demo - QA (https://orge821e2a2.crm.dynamics.com/)

Use --environment to specify a common target.
```

```
Error: Profile 'app2' has no environment selected.

Either:
  1. Select environment: ppds auth select app2 && ppds env select --environment "Prod"
  2. Specify on command: --environment "Prod"
```

---

## Global Options

Available on all commands:

```
  -v, --verbose       Verbose output
  --debug             Debug output (includes timing, internal state)
  --json              JSON output format (for scripting)
  -h, --help          Show help
  --version           Show version
```

---

## Exit Codes

| Code | Meaning |
|------|---------|
| 0 | Success |
| 1 | General error |
| 2 | Authentication error |
| 3 | Invalid arguments |
| 4 | Environment not found |
| 5 | Connection error |
| 130 | Cancelled (Ctrl+C) |

---

## Environment Variables

For CI/CD scenarios, credentials can be provided via environment variables:

| Variable | Description |
|----------|-------------|
| `PPDS_PROFILE` | Default profile name |
| `PPDS_ENVIRONMENT` | Default environment |
| `DATAVERSE__URL` | Dataverse URL (legacy compatibility) |
| `DATAVERSE__CLIENTID` | Application ID |
| `DATAVERSE__CLIENTSECRET` | Client secret |
| `DATAVERSE__TENANTID` | Tenant ID |

**Note:** Environment variables are lower priority than explicit CLI flags and stored profiles.

---

## Migration from PPDS.Migration.Cli

### Command Mapping

| Old Command | New Command |
|-------------|-------------|
| `ppds-migrate export` | `ppds data export` |
| `ppds-migrate import` | `ppds data import` |
| `ppds-migrate migrate` | `ppds data copy` |
| `ppds-migrate analyze` | `ppds data analyze` |
| `ppds-migrate schema generate` | `ppds schema generate` |
| `ppds-migrate schema list` | `ppds schema list` |
| `ppds-migrate users generate` | `ppds users generate` |

### Auth Migration

Old model:
```bash
ppds-migrate export --url https://org.crm.dynamics.com --auth interactive --schema schema.xml
```

New model:
```bash
# One-time setup
ppds auth create --name dev --deviceCode --environment "PPDS Demo - Dev"

# Commands use profile
ppds data export --schema schema.xml --output data.zip
```

### Breaking Changes

1. Tool name: `ppds-migrate` → `ppds`
2. Command structure: Flat → Nested (`export` → `data export`)
3. Auth: Per-command flags → Stored profiles
4. Environment: `--url` → `--environment` (with discovery)

---

## Implementation Phases

### Phase 1: Foundation (Week 1)
- [ ] Create PPDS.Auth package
- [ ] Profile storage with encryption
- [ ] Cloud environment support
- [ ] Device Code credential (port from existing)
- [ ] Client Secret credential
- [ ] `auth create`, `auth list`, `auth who`

### Phase 2: Environment Discovery (Week 1-2)
- [ ] Global Discovery Service client
- [ ] `env list`, `env select`, `env who`
- [ ] Environment resolution (name, ID, URL, partial)

### Phase 3: CLI Restructure (Week 2)
- [ ] Create PPDS.Cli package
- [ ] Command hierarchy (auth, env, data, schema, users)
- [ ] Port existing commands to new structure
- [ ] Global options and output formatting

### Phase 4: Additional Auth Methods (Week 2-3)
- [ ] Certificate from file
- [ ] Certificate from store
- [ ] Managed Identity
- [ ] GitHub OIDC
- [ ] Azure DevOps OIDC
- [ ] Username/Password (deprecated)
- [ ] `auth update`, `auth delete`, `auth name`, `auth clear`, `auth select`

### Phase 5: Pooling Integration (Week 3)
- [ ] Multiple `--profile` support
- [ ] Environment validation for pooled profiles
- [ ] Integration with DataverseConnectionPool

### Phase 6: Polish (Week 3)
- [ ] Testing
- [ ] Documentation
- [ ] Migration guide

---

## File Structure

```
PPDS.Auth/
├── Credentials/
│   ├── ICredentialProvider.cs
│   ├── DeviceCodeCredential.cs
│   ├── ClientSecretCredential.cs
│   ├── CertificateCredential.cs
│   ├── ManagedIdentityCredential.cs
│   ├── GitHubOidcCredential.cs
│   ├── AzureDevOpsOidcCredential.cs
│   └── UsernamePasswordCredential.cs
├── Discovery/
│   ├── IGlobalDiscoveryService.cs
│   ├── GlobalDiscoveryService.cs
│   └── EnvironmentInfo.cs
├── Profiles/
│   ├── AuthProfile.cs
│   ├── ProfileStore.cs
│   ├── ProfileEncryption.cs
│   └── ProfileManager.cs
├── Cloud/
│   ├── CloudEnvironment.cs
│   └── CloudEndpoints.cs
└── ServiceClientFactory.cs

PPDS.Cli/
├── Commands/
│   ├── Auth/
│   │   ├── CreateCommand.cs
│   │   ├── ListCommand.cs
│   │   ├── SelectCommand.cs
│   │   ├── DeleteCommand.cs
│   │   ├── UpdateCommand.cs
│   │   ├── NameCommand.cs
│   │   ├── ClearCommand.cs
│   │   └── WhoCommand.cs
│   ├── Env/
│   │   ├── ListCommand.cs
│   │   ├── SelectCommand.cs
│   │   └── WhoCommand.cs
│   ├── Data/
│   │   ├── ExportCommand.cs
│   │   ├── ImportCommand.cs
│   │   ├── CopyCommand.cs
│   │   └── AnalyzeCommand.cs
│   ├── Schema/
│   │   ├── GenerateCommand.cs
│   │   └── ListCommand.cs
│   └── Users/
│       └── GenerateCommand.cs
├── Infrastructure/
│   ├── GlobalOptions.cs
│   ├── OutputFormatter.cs
│   ├── ExitCodes.cs
│   └── ConnectionResolver.cs
├── Program.cs
└── PPDS.Cli.csproj
```

---

## Dependencies

### PPDS.Auth

```xml
<ItemGroup>
  <PackageReference Include="Azure.Identity" Version="1.13.*" />
  <PackageReference Include="Microsoft.Identity.Client" Version="4.67.*" />
  <PackageReference Include="Microsoft.Identity.Client.Extensions.Msal" Version="4.67.*" />
  <PackageReference Include="Microsoft.PowerPlatform.Dataverse.Client" Version="1.2.*" />
  <PackageReference Include="System.Text.Json" Version="9.0.*" />
</ItemGroup>
```

### PPDS.Cli

```xml
<ItemGroup>
  <PackageReference Include="System.CommandLine" Version="2.0.0-beta4.*" />
  <ProjectReference Include="..\PPDS.Auth\PPDS.Auth.csproj" />
  <ProjectReference Include="..\PPDS.Migration\PPDS.Migration.csproj" />
  <ProjectReference Include="..\PPDS.Dataverse\PPDS.Dataverse.csproj" />
</ItemGroup>
```

---

## Open Questions

1. **Secrets file provider?** Should we support credential files like `~/.ppds/credentials.json` for CI scenarios where env vars are inconvenient?

2. **Profile import/export?** Should profiles be exportable (with secrets redacted) for team sharing?

3. **Connection string fallback?** Should we support raw connection strings for advanced users?

---

## References

- [PAC CLI Documentation](https://learn.microsoft.com/en-us/power-platform/developer/cli/introduction)
- [Dataverse Global Discovery Service](https://learn.microsoft.com/en-us/power-apps/developer/data-platform/webapi/discover-url-organization-web-api)
- [Azure.Identity Credential Chains](https://learn.microsoft.com/en-us/dotnet/azure/sdk/authentication/credential-chains)
- [MSAL.NET Token Caching](https://learn.microsoft.com/en-us/entra/msal/dotnet/how-to/token-cache-serialization)
