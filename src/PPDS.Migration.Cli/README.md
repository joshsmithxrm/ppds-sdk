# PPDS.Migration.Cli

High-performance Dataverse data migration CLI tool. Part of the [PPDS SDK](../../README.md).

## Installation

```bash
# Global install
dotnet tool install --global PPDS.Migration.Cli

# Local install (in project)
dotnet tool install PPDS.Migration.Cli

# Verify
ppds-migrate --version
```

## Commands

| Command | Description |
|---------|-------------|
| `export` | Export data from Dataverse to a ZIP file |
| `import` | Import data from a ZIP file into Dataverse |
| `analyze` | Analyze schema and display dependency graph |
| `migrate` | Migrate data from source to target environment |

## Usage

### Export

```bash
ppds-migrate export \
  --connection "AuthType=ClientSecret;Url=https://org.crm.dynamics.com;ClientId=xxx;ClientSecret=xxx" \
  --schema ./schema.xml \
  --output ./data.zip
```

### Import

```bash
ppds-migrate import \
  --connection "AuthType=ClientSecret;Url=https://org.crm.dynamics.com;ClientId=xxx;ClientSecret=xxx" \
  --data ./data.zip \
  --bypass-plugins
```

### Analyze

```bash
ppds-migrate analyze --schema ./schema.xml --output-format json
```

### Migrate

```bash
ppds-migrate migrate \
  --source "AuthType=ClientSecret;Url=https://source.crm.dynamics.com;..." \
  --target "AuthType=ClientSecret;Url=https://target.crm.dynamics.com;..." \
  --schema ./schema.xml
```

## Security: Environment Variables

Connection strings contain sensitive credentials. To avoid exposing them in command-line arguments (which may appear in process listings or shell history), use environment variables:

| Environment Variable | Used By | Description |
|---------------------|---------|-------------|
| `PPDS_CONNECTION` | `export`, `import` | Default connection string |
| `PPDS_SOURCE_CONNECTION` | `migrate` | Source environment connection |
| `PPDS_TARGET_CONNECTION` | `migrate` | Target environment connection |

**Example using environment variables:**

```bash
# Set credentials once (in your CI/CD pipeline or shell profile)
export PPDS_CONNECTION="AuthType=ClientSecret;Url=https://org.crm.dynamics.com;ClientId=xxx;ClientSecret=xxx"

# Commands use environment variable automatically
ppds-migrate export --schema ./schema.xml --output ./data.zip
ppds-migrate import --data ./data.zip --bypass-plugins
```

**Example for migration:**

```bash
export PPDS_SOURCE_CONNECTION="AuthType=ClientSecret;Url=https://source.crm.dynamics.com;..."
export PPDS_TARGET_CONNECTION="AuthType=ClientSecret;Url=https://target.crm.dynamics.com;..."

ppds-migrate migrate --schema ./schema.xml
```

**Priority:** Command-line arguments take precedence over environment variables.

## Exit Codes

| Code | Meaning |
|------|---------|
| 0 | Success |
| 1 | Partial success (some records failed) |
| 2 | Failure (operation could not complete) |
| 3 | Invalid arguments |

## JSON Progress Output

The `--json` flag enables structured JSON output for tool integration. This format is a **public contract** used by [PPDS.Tools](https://github.com/joshsmithxrm/ppds-tools) PowerShell cmdlets and potentially other integrations.

```bash
ppds-migrate export --connection "..." --schema ./schema.xml --output ./data.zip --json
```

**Output format (one JSON object per line):**

```json
{"phase":"analyzing","message":"Parsing schema...","timestamp":"2025-12-19T10:30:00Z"}
{"phase":"export","entity":"account","current":450,"total":1000,"rps":287.5,"timestamp":"2025-12-19T10:30:15Z"}
{"phase":"complete","duration":"00:05:23","recordsProcessed":1505,"errors":0,"timestamp":"2025-12-19T10:35:23Z"}
```

**Phases:**

| Phase | Fields | Description |
|-------|--------|-------------|
| `analyzing` | `message` | Schema parsing and dependency analysis |
| `export` | `entity`, `current`, `total`, `rps` | Exporting entity data |
| `import` | `entity`, `current`, `total`, `rps`, `tier` | Importing entity data |
| `deferred` | `entity`, `field`, `current`, `total` | Updating deferred lookup fields |
| `complete` | `duration`, `recordsProcessed`, `errors` | Operation finished |
| `error` | `message` | Error occurred |

## Related

- [PPDS.Tools](https://github.com/joshsmithxrm/ppds-tools) - PowerShell cmdlets that wrap this CLI
- [PPDS.Dataverse](../PPDS.Dataverse/) - High-performance Dataverse connectivity
