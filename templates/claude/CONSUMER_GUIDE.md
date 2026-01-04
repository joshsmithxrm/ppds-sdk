# PPDS Development Guide

Best practices for Power Platform development using PPDS tools.

---

## Plugin Development

- Use `[PluginStep]` and `[PluginImage]` attributes from `PPDS.Plugins`
- Run `ppds plugins extract` to generate registration config from assembly
- Deploy with `ppds plugins deploy --solution MySolution`
- Use `--dry-run` to preview changes before applying

```bash
# Extract registrations from compiled assembly
ppds plugins extract --assembly bin/Debug/MyPlugins.dll --output registrations.json

# Preview what will change
ppds plugins deploy --config registrations.json --dry-run

# Deploy to environment
ppds plugins deploy --config registrations.json --solution MySolution
```

---

## Data Operations

### Schema Generation

```bash
# Generate schema for specific entities
ppds data schema --entities account,contact,opportunity --output schema.xml

# Include all custom fields
ppds data schema --entities account --include-custom-fields
```

### Export/Import

```bash
# Export data based on schema
ppds data export --schema schema.xml --output data.zip

# Import data with user mapping
ppds data import --data data.zip --user-mapping users.json
```

### CSV Loading

```bash
# Generate mapping template (auto-matches columns)
ppds data load --entity account --file accounts.csv --generate-mapping

# Validate without writing
ppds data load --entity account --file accounts.csv --mapping mapping.json --dry-run

# Load with upsert on alternate key
ppds data load --entity account --file accounts.csv --key accountnumber
```

---

## Querying & Troubleshooting

### SQL Queries

```bash
# Simple query
ppds query sql "SELECT name, revenue FROM account WHERE statecode = 0"

# With aggregates
ppds query sql "SELECT ownerid, COUNT(*) as total FROM account GROUP BY ownerid"

# Output to CSV
ppds query sql "SELECT * FROM contact" -f csv > contacts.csv
```

### FetchXML

```bash
# From file
ppds query fetch --file my-query.xml

# Inline
ppds query fetch "<fetch><entity name='account'><attribute name='name'/></entity></fetch>"
```

### Metadata Browser

```bash
# List custom entities
ppds metadata entities --custom-only

# List lookup fields on an entity
ppds metadata attributes account --type Lookup

# View entity relationships
ppds metadata relationships account
```

---

## Authentication

### Profile Management

```bash
# Create interactive profile (opens browser)
ppds auth create --name dev

# Create service principal profile
ppds auth create --name ci --applicationId <guid> --clientSecret <secret> --tenant <tenant>

# Select environment
ppds env select

# Check current identity
ppds auth who
```

### CI/CD

For service principals in CI/CD, set the `PPDS_SPN_SECRET` environment variable:

```yaml
env:
  PPDS_SPN_SECRET: ${{ secrets.DATAVERSE_CLIENT_SECRET }}
```

---

## Best Practices

### Do

- Use `--dry-run` before destructive operations in production
- Use connection pooling with multiple profiles for high-throughput scenarios
- Use early-bound entity classes from `PPDS.Dataverse.Generated`
- Store plugin registrations in source control (`registrations.json`)

### Never

- Commit credentials or `.env` files
- Use magic strings for entity/attribute names (use constants)
- Create new `ServiceClient` per request (use pool)
- Run deploy/import without `--dry-run` first in production environments
