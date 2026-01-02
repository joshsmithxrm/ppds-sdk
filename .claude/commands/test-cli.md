# CLI Smoke Test

Installs and tests the local CLI to verify changes work correctly.

## Usage

`/test-cli`

## What It Does

### 1. Analyze Changes
- Read recent commits and changelog to understand what changed
- Identify which commands/features need testing
- If unclear what to test, ask the user

### 2. Install Latest
```powershell
pwsh -ExecutionPolicy Bypass -File ./scripts/Install-LocalCli.ps1
```

### 3. Verify Version
```bash
ppds --version
```
- Confirm version matches expected commit hash from `git rev-parse HEAD`

### 4. Run Tests

#### Baseline Tests (always run)
```bash
ppds --version              # Version displays correctly
ppds --help                 # Help works
ppds auth --help            # Subcommand help works
ppds env --help
ppds data --help
ppds plugins --help
```

#### Change-Specific Tests
Based on what changed, test:
- **New flags/options**: Verify they work
- **Removed flags/options**: Verify they fail gracefully
- **Moved commands**: Verify new paths work, old paths don't exist
- **New commands**: Verify --help and basic invocation
- **Output format changes**: Verify JSON/Text output

#### Commands Requiring Auth
Use the `cli-test` profile for any command requiring authentication:
```bash
ppds env list --profile cli-test -f Json
ppds auth who --profile cli-test
```

**IMPORTANT**:
- ONLY use `--profile cli-test` - never use default profile or other profiles
- If `cli-test` profile doesn't exist, SKIP auth-requiring tests and note it
- Never run destructive commands (delete, clean without --what-if)

### 5. Report Results

```
CLI Smoke Test Results
======================
Version: 1.0.0-beta.5.4+d750e7d (expected: d750e7d) ✓

Baseline Tests:
  [✓] ppds --version
  [✓] ppds --help
  [✓] ppds auth --help
  [✓] ppds env --help
  [✓] ppds data --help
  [✓] ppds plugins --help

Change-Specific Tests (#73, #74):
  [✓] ppds auth list -f Json (new flag works)
  [✓] ppds auth list --json (old flag properly removed)
  [✓] ppds data schema --help (moved command exists)
  [✗] ppds schema generate (old path still works - SHOULD FAIL)

Auth Tests (using cli-test profile):
  [✓] ppds auth who --profile cli-test
  [✓] ppds env list --profile cli-test -f Json

Summary: 11/12 passed, 1 failed
```

## Behavior

- **On test failure**: Report which test failed, show actual output, continue testing
- **On install failure**: Stop and report
- **On version mismatch**: Warn but continue
- **Profile missing**: Skip auth tests with note, don't fail

## When to Use

- After making CLI changes
- Before creating a PR (can run after `/pre-pr`)
- When verifying a fix works end-to-end
- After pulling changes that affect CLI

## Safe Commands Only

This command will NEVER:
- Use any profile other than `cli-test`
- Run data export/import against real environments
- Delete or modify Dataverse data
- Run `ppds plugins clean` without `--what-if`
- Run `ppds plugins deploy` without `--what-if`
