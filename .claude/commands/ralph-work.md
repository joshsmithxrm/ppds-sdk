---
description: Start autonomous Ralph Wiggum session for an issue
arguments:
  - name: issue
    description: GitHub issue number
    required: true
  - name: max-iterations
    description: Maximum loop iterations (default 50)
    required: false
---

# Ralph Work Session

Start an autonomous work session using Ralph Wiggum for issue #$issue.

## Setup

1. Fetch issue details:
```bash
gh issue view $issue --json title,body,labels
```

2. Create session file at `~/.ppds/sessions/work-$issue.json`:
```powershell
$sessionDir = "$env:USERPROFILE/.ppds/sessions"
New-Item -ItemType Directory -Path $sessionDir -Force | Out-Null
@{
  id = "work-$issue"
  status = "working"
  issue = "#$issue"
  branch = (git branch --show-current)
  worktree = (Get-Location).Path
  started = (Get-Date -Format o)
  lastUpdate = (Get-Date -Format o)
  stuck = $null
  guidance = $null
} | ConvertTo-Json | Set-Content "$sessionDir/work-$issue.json"
```

## Start Ralph Loop

Execute the Ralph loop with the full worker prompt from `scripts/ralph-prompt-template.md`.

Key behaviors:
- Update session file at start of each iteration
- Check for `guidance` field - if present, follow it and clear it
- Set `status: stuck` when hitting domain gates or failing 5x
- Set `status: pr_ready` when PR created
- Output `<promise>PR_READY</promise>` on success
- Output `<promise>STUCK_NEEDS_HUMAN</promise>` when blocked

Max iterations: ${max-iterations:-50}

## Workflow

1. **Plan**: Read issue, explore codebase, design approach
2. **Implement**: Follow PPDS patterns (CLAUDE.md)
3. **Test**: Run `/test`, loop until passing (max 5 attempts per failure)
4. **Ship**: Run `/ship` for PR creation and CI handling

## Domain Gates (MUST escalate)

Set status='stuck' and output `<promise>STUCK_NEEDS_HUMAN</promise>` when touching:
- **Auth/Security**: Token handling, credentials, permissions
- **Performance-critical**: Bulk operations, connection pooling, parallelism
- **Breaking changes**: Public API modifications
- **Data migration**: Schema changes

## Exit Conditions

- `<promise>PR_READY</promise>` - PR created, CI green (or 3 CI-fix attempts)
- `<promise>STUCK_NEEDS_HUMAN</promise>` - Domain gate, or failed 5x on same issue
- `<promise>BLOCKED_EXTERNAL</promise>` - External dependency blocking progress
