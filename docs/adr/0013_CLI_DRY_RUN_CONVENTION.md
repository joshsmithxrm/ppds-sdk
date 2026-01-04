# ADR-0013: CLI Dry-Run Convention

**Status:** Accepted
**Date:** 2026-01-04
**Authors:** Josh, Claude

## Context

The CLI uses preview mode flags to show what operations would be performed without actually executing them. This is essential for production safety and allows users to verify changes before committing to them.

However, the codebase had inconsistent naming:
- `ppds plugins deploy --what-if` (PowerShell convention)
- `ppds plugins clean --what-if` (PowerShell convention)
- `ppds data load --dry-run` (Unix convention)

This inconsistency creates user confusion and violates the principle of least surprise.

## Decision

**Standardize on `--dry-run` for all PPDS.Cli commands.**

```bash
# All CLI commands use --dry-run
ppds plugins deploy --config registrations.json --dry-run
ppds plugins clean --config registrations.json --dry-run
ppds data load --entity account --file accounts.csv --dry-run
ppds data import --data data.zip --dry-run
```

## Rationale

### 1. PPDS.Cli is a Unix-style CLI

PPDS.Cli is built with System.CommandLine and follows Unix CLI conventions:
- Double-dash long options (`--config`, not `/config`)
- Lowercase commands (`ppds data export`, not `ppds Data Export`)
- No verb-noun cmdlet pattern

Unix CLIs universally use `--dry-run`:
- `rsync --dry-run`
- `rm --interactive` (similar cautionary flag)
- `make --dry-run`

### 2. .NET CLI uses `--dry-run`

The .NET CLI itself uses `--dry-run`:
```bash
dotnet nuget push --dry-run
dotnet tool update --dry-run
```

Since PPDS.Cli is a .NET global tool, following .NET CLI conventions provides consistency.

### 3. Cross-platform CLIs use `--dry-run`

Major cross-platform CLIs use `--dry-run`:
- AWS CLI: `--dry-run`
- Docker: `--dry-run` (in compose)
- kubectl: `--dry-run`
- Terraform: `plan` command (equivalent concept)

### 4. PowerShell module will use `-WhatIf`

PPDS.Tools (the PowerShell module) will correctly use `-WhatIf` following PowerShell conventions:
```powershell
Deploy-DataversePlugin -Config registrations.json -WhatIf
```

This is the right convention for PowerShell cmdlets and aligns with the PowerShell ecosystem.

### 5. Two interfaces, two conventions

The separation is intentional:
- **CLI users** expect Unix conventions (`--dry-run`)
- **PowerShell users** expect PowerShell conventions (`-WhatIf`)

Each interface follows its ecosystem's conventions, providing a native experience for both user groups.

## Consequences

### Positive

- Consistent experience across all CLI commands
- Aligns with .NET CLI and Unix conventions
- Clear separation between CLI and PowerShell patterns
- Reduces cognitive load for CLI users

### Negative

- Breaking change for scripts using `--what-if`
- Requires updating any documentation referencing `--what-if`

### Migration

Since PPDS.Cli is in pre-release (beta), this breaking change is acceptable. Scripts using `--what-if` will receive a clear error message when the flag is not recognized, prompting users to update to `--dry-run`.

## Console Output

When `--dry-run` is active, commands display:
```
[Dry-Run Mode] No changes will be applied.
```

Individual operations show:
```
  [Dry-Run] Would create step: MyPlugin.PreCreate: Create of account
  [Dry-Run] Would update image: PreImage
```

## References

- [.NET CLI documentation](https://learn.microsoft.com/en-us/dotnet/core/tools/)
- [System.CommandLine](https://learn.microsoft.com/en-us/dotnet/standard/commandline/)
- [PowerShell ShouldProcess](https://learn.microsoft.com/en-us/powershell/scripting/learn/deep-dives/everything-about-shouldprocess)
