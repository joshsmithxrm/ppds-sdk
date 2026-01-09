# Autonomous Work Session - Issue #{ISSUE_NUMBER}

## Context

**Issue**: #{ISSUE_NUMBER} - {ISSUE_TITLE}
**Branch**: {BRANCH_NAME}
**Worktree**: {WORKTREE_PATH}
**Related**: {RELATED_ISSUES}

### Issue Description
{ISSUE_BODY}

---

## Session File Contract

You MUST update your session status file at key points using Bash tool with PowerShell.

### Session File Location
`~/.ppds/sessions/work-{ISSUE_NUMBER}.json`

### Status Updates

**At start of each iteration:**
```powershell
$s = @{id='work-{ISSUE_NUMBER}';status='working';issue='#{ISSUE_NUMBER}';branch='{BRANCH_NAME}';worktree='{WORKTREE_PATH}';started='{STARTED_ISO}';lastUpdate=(Get-Date -Format o);stuck=$null} | ConvertTo-Json
$s | Set-Content "$env:USERPROFILE/.ppds/sessions/work-{ISSUE_NUMBER}.json"
```

**When stuck (after 5 failed attempts OR domain gate):**
```powershell
$s = Get-Content "$env:USERPROFILE/.ppds/sessions/work-{ISSUE_NUMBER}.json" | ConvertFrom-Json
$s.status = 'stuck'
$s.lastUpdate = (Get-Date -Format o)
$s.stuck = @{reason='DESCRIBE_BLOCKER';context='WHAT_YOU_TRIED';options=@('option1','option2');since=(Get-Date -Format o)}
$s | ConvertTo-Json -Depth 3 | Set-Content "$env:USERPROFILE/.ppds/sessions/work-{ISSUE_NUMBER}.json"
```

**When PR ready:**
```powershell
$s = Get-Content "$env:USERPROFILE/.ppds/sessions/work-{ISSUE_NUMBER}.json" | ConvertFrom-Json
$s.status = 'pr_ready'
$s.lastUpdate = (Get-Date -Format o)
$s.prUrl = 'PR_URL_HERE'
$s.stuck = $null
$s | ConvertTo-Json | Set-Content "$env:USERPROFILE/.ppds/sessions/work-{ISSUE_NUMBER}.json"
```

### Checking for Human Guidance

If your status is 'stuck', check if human provided guidance:
```powershell
$s = Get-Content "$env:USERPROFILE/.ppds/sessions/work-{ISSUE_NUMBER}.json" | ConvertFrom-Json
if ($s.guidance) { Write-Output "Human guidance: $($s.guidance)" }
```

If guidance exists, follow it, then clear guidance and set status back to 'working'.

---

## Your Workflow

### Phase 1: Plan
1. Read the issue thoroughly
2. Explore codebase to understand affected areas
3. Design your implementation approach
4. Write plan to `.claude/session-prompt.md` in this worktree

### Phase 2: Implement
1. Follow PPDS patterns (see CLAUDE.md)
2. Use early-bound entities with `EntityLogicalName` and `Fields.*`
3. Use Application Services for business logic
4. Accept `IProgressReporter` for long operations

### Phase 3: Test
Run tests using `/test` command (auto-detects test type):
- Unit tests: default
- TUI tests: if TUI files changed
- Integration: only if explicitly requested

**Test-Fix Loop Rules:**
- Max 5 attempts on same failure before escalating
- If stuck 3x on SAME error, update status to 'stuck'

### Phase 4: Ship
Run `/ship` command which handles:
- Commit with proper message
- Push to remote
- Create PR
- Monitor CI (max 3 fix attempts)
- Handle bot comments

---

## Domain Gates (MUST ESCALATE)

Stop and set status='stuck' when touching:

| Gate | Examples |
|------|----------|
| **Auth/Security** | Token handling, credentials, permissions, encryption |
| **Performance-critical** | Bulk operations, connection pooling, parallelism (DOP) |
| **Breaking changes** | Public API modifications, removing/renaming exports |
| **Data migration** | Schema changes, data transformation logic |

Set stuck.reason to describe the gate and what decision you need.

---

## Completion Criteria

ALL must be true:
- [ ] Implementation complete per issue requirements
- [ ] All unit tests passing
- [ ] `/ship` completed successfully
- [ ] PR created with CI green (or 3 CI-fix attempts made)

---

## Exit Conditions

### Success: Output `<promise>PR_READY</promise>`
When:
- PR is created AND CI is green
- OR PR is created AND you've made 3 CI fix attempts

Before outputting, update session file to status='pr_ready'.

### Stuck: Output `<promise>STUCK_NEEDS_HUMAN</promise>`
When:
- Domain gate hit (Auth/Security, Performance, etc.)
- Same test failure 5x
- Same CI failure 3x
- Requirements unclear, cannot proceed

Before outputting, update session file to status='stuck' with context.

### Blocked: Output `<promise>BLOCKED_EXTERNAL</promise>`
When:
- Blocked by another PR that must merge first
- Need infrastructure/access you don't have
- External dependency issue

Before outputting, update session file to status='blocked'.

---

## Important Rules

1. **Never guess** - If unclear, set stuck and ask
2. **No scope creep** - Only implement what the issue asks
3. **Test everything** - If you change it, test it
4. **Document gates** - Always explain WHY you're stuck
5. **Check guidance** - At start of each iteration, check if human provided guidance in session file

---

## Self-Verification Checklist (Before Shipping)

- [ ] Re-read original issue - does solution match?
- [ ] CLAUDE.md rules followed?
- [ ] No magic strings for entities?
- [ ] Bulk APIs used for multi-record ops?
- [ ] CLI smoke test if CLI changed: `ppds --version`
