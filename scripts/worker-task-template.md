# Task Worker Template

You are a PPDS worker session implementing GitHub issue #{ISSUE_NUMBER}.

## Issue Details
- **Title**: {ISSUE_TITLE}
- **Body**: {ISSUE_BODY}
- **Branch**: {BRANCH_NAME}

## Session File
Location: `~/.ppds/sessions/work-{ISSUE_NUMBER}.json`

Update this file at the start of your work and when status changes.

## Workflow

### 1. Initialize Session
```powershell
$session = @{
    id = "work-{ISSUE_NUMBER}"
    status = "working"
    issue = "#{ISSUE_NUMBER}"
    branch = "{BRANCH_NAME}"
    started = (Get-Date -Format "o")
    lastUpdate = (Get-Date -Format "o")
    stuck = $null
    guidance = $null
    cancelRequested = $false
    prUrl = $null
} | ConvertTo-Json
$session | Set-Content "$env:USERPROFILE/.ppds/sessions/work-{ISSUE_NUMBER}.json"
```

### 2. Check for Guidance/Cancellation
Before major work, check if orchestrator sent guidance or cancel:
```powershell
$session = Get-Content "$env:USERPROFILE/.ppds/sessions/work-{ISSUE_NUMBER}.json" | ConvertFrom-Json
if ($session.cancelRequested) {
    # Exit gracefully
}
if ($session.guidance) {
    # Apply guidance, then clear it
}
```

### 3. Implement
Follow PPDS patterns from CLAUDE.md:
- Use early-bound entities (not late-bound)
- Use connection pool for multi-request scenarios
- Accept `IProgressReporter` for long operations
- Wrap errors in `PpdsException`

### 4. Domain Gates
STOP and escalate (set stuck status) when touching:
- **Auth/Security** - Token handling, credentials, permissions
- **Performance-critical** - Bulk operations, DOP values
- **Breaking changes** - Public API modifications
- **Data migration** - Schema changes

To escalate:
```powershell
$session.status = "stuck"
$session.stuck = @{
    reason = "Auth/Security decision needed"
    context = "Token refresh approach unclear"
    options = @("sliding", "fixed")
    since = (Get-Date -Format "o")
}
$session.lastUpdate = (Get-Date -Format "o")
$session | ConvertTo-Json -Depth 3 | Set-Content "$env:USERPROFILE/.ppds/sessions/work-{ISSUE_NUMBER}.json"
```

Then WAIT. The orchestrator will provide guidance.

### 5. Test
Run `/test` command. If tests fail:
- Fix and retry (up to 5 attempts per unique failure)
- After 5 attempts on same failure, escalate as stuck

### 6. Ship
Run `/ship` command to commit, push, create PR.

On success, update session:
```powershell
$session.status = "pr_ready"
$session.prUrl = "https://github.com/.../pull/N"
$session.lastUpdate = (Get-Date -Format "o")
$session | ConvertTo-Json -Depth 3 | Set-Content "$env:USERPROFILE/.ppds/sessions/work-{ISSUE_NUMBER}.json"
```

## Status Values

| Status | Meaning |
|--------|---------|
| `working` | Actively implementing |
| `stuck` | Needs human guidance (check `stuck` object) |
| `pr_ready` | PR created, work complete |
| `blocked` | External dependency blocking |
| `cancelled` | User requested cancellation |

## Heartbeat
Update `lastUpdate` periodically so orchestrator knows you're alive:
```powershell
$session.lastUpdate = (Get-Date -Format "o")
```

If no update for 10+ minutes, orchestrator may flag you as stale/crashed.
