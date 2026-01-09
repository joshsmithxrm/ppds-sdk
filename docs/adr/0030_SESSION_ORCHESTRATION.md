# ADR-0030: Session Orchestration with Ralph Wiggum

## Status
Accepted (Revised)

## Context
The `/plan-work` orchestration system was designed in workflow documentation but never implemented. The goal is parallel Claude Code sessions working on multiple issues simultaneously, with human oversight for stuck sessions.

Key requirements:
1. Spawn N worker sessions for GitHub issues
2. Each session works autonomously on an issue
3. Sessions coordinate via status files
4. Human receives prompts when sessions get stuck
5. Sessions complete when PRs are created

Ralph Wiggum is an official Claude Code plugin that creates autonomous development loops using a Stop hook pattern.

## Decision
Implement session orchestration using **Claude-as-orchestrator** with hybrid workers:

1. **Claude orchestrator session** - human interacts via natural language
2. **Task agent workers** - simple tasks via `Task` tool with `run_in_background`
3. **Ralph terminal workers** - complex tasks via terminal tabs with Ralph loops
4. **File-based coordination** - `~/.ppds/sessions/*.json`

### Why Claude-as-Orchestrator?
The original PowerShell orchestrator required terminal interaction. Claude-as-orchestrator enables:
- Natural language commands ("add issue 123", "status", "guide 456: use Option A")
- Integrated monitoring within the same conversation
- Stateless recovery on restart

### Why Hybrid Workers?
**Task agents cannot run Ralph Wiggum loops** - Ralph requires an interactive terminal session. Therefore:

| Worker Type | Spawning | Use Case |
|-------------|----------|----------|
| Task agent | `Task` tool, `run_in_background: true` | Simple, well-defined tasks |
| Ralph terminal | Bash → `wt` command | Complex, exploratory, multi-phase |

### Architecture
```
User (natural language)
  |
  v
Orchestrator Claude Session
  |-- Parses: "add 123", "status?", "guide 456: use Option A"
  |-- Reads: ~/.ppds/sessions/*.json (stateless)
  |-- Writes: ~/.ppds/sessions/orchestrator.json
  |
  +---> Task Worker (background)
  |       |-- Updates work-123.json
  |       |-- Returns output to orchestrator
  |
  +---> Ralph Worker (terminal tab)
          |-- Updates work-456.json
          |-- Outputs <promise>PR_READY</promise>
```

### Session File Schemas

**orchestrator.json** - Worker registry
```json
{
  "workers": {
    "123": {
      "type": "task",
      "taskId": "abc123",
      "outputFile": "/path/to/output.txt",
      "sessionFile": "~/.ppds/sessions/work-123.json",
      "started": "2025-01-08T10:00:00Z"
    },
    "456": {
      "type": "ralph",
      "terminalTitle": "Issue #456",
      "sessionFile": "~/.ppds/sessions/work-456.json",
      "started": "2025-01-08T10:05:00Z"
    }
  }
}
```

**work-{issue}.json** - Worker status
```json
{
  "id": "work-123",
  "status": "working|stuck|pr_ready|blocked|cancelled",
  "issue": "#123",
  "branch": "issue-123",
  "worktree": "C:/VS/ppds-issue-123",
  "started": "2025-01-08T10:30:00Z",
  "lastUpdate": "2025-01-08T11:15:00Z",
  "stuck": {
    "reason": "Auth/Security decision needed",
    "context": "Token refresh approach unclear",
    "options": ["sliding", "fixed"],
    "since": "2025-01-08T11:00:00Z"
  },
  "guidance": "Use sliding expiration with 15-minute window",
  "cancelRequested": false,
  "prUrl": "https://github.com/.../pull/42"
}
```

### Ralph Wiggum Integration
Each worker runs:
```bash
/ralph-loop --file "$promptFile" --max-iterations 50 --completion-promise PR_READY
```

The prompt instructs Claude to:
1. Update session file at iteration start
2. Check for human guidance
3. Follow PPDS patterns (CLAUDE.md)
4. Escalate on domain gates (Auth/Security, Performance)
5. Run `/test` and `/ship` commands
6. Output completion promise when done

### Completion Promises
| Promise | Meaning |
|---------|---------|
| `<promise>PR_READY</promise>` | PR created, CI passed (or 3 attempts) |
| `<promise>STUCK_NEEDS_HUMAN</promise>` | Domain gate or 5x same failure |
| `<promise>BLOCKED_EXTERNAL</promise>` | External dependency blocking |

### Domain Gates
Sessions MUST escalate (set stuck status) when touching:
- **Auth/Security** - Token handling, credentials, permissions
- **Performance-critical** - Bulk operations, connection pooling, parallelism
- **Breaking changes** - Public API modifications
- **Data migration** - Schema changes

## Consequences

### Positive
- Parallel development on multiple issues
- Human oversight without constant monitoring
- Natural language interaction (no terminal prompts)
- Clear escalation paths for sensitive areas
- File-based coordination is simple and debuggable
- Stateless orchestrator - survives restarts
- Hybrid workers: simple tasks fast, complex tasks autonomous

### Negative
- Ralph terminal workers require Windows Terminal (Windows-only)
- Task agent workers have no interactive Ralph loops
- No unified service layer yet (V2: `ISessionService`)

### Neutral
- Session files in `~/.ppds/sessions/` consistent with ADR-0024
- PowerShell script (`Start-PpdsOrchestration.ps1`) remains as legacy/fallback

## Files

| File | Purpose |
|------|---------|
| `.claude/commands/orchestrate.md` | `/orchestrate` slash command |
| `scripts/worker-task-template.md` | Task agent worker template |
| `scripts/ralph-prompt-template.md` | Ralph terminal worker template |
| `scripts/Start-PpdsOrchestration.ps1` | Legacy PowerShell orchestrator |
| `.claude/commands/ralph-work.md` | Single-session slash command |

## Resilience

### Restart Recovery
Orchestrator is stateless - reads session files on each status check:
1. Workers continue independently (terminals/tasks)
2. Orchestrator reads `orchestrator.json` for registered workers
3. Orchestrator reads `work-*.json` for current status
4. User says "status" → orchestrator resumes

### Staleness Detection
Workers update `lastUpdate` each iteration. Orchestrator flags stale after 10 minutes.

### Cancellation
Set `cancelRequested: true` in session file. Workers check at iteration start.

## Future Enhancements (V2)
- **ISessionService**: Proper C# service layer
- **ppds serve integration**: RPC server for VS Code extension
- **VS Code dashboard**: Visual session monitoring
- **Cross-platform**: Bash script for macOS/Linux Ralph spawning

## References
- [Ralph Wiggum Plugin](https://github.com/anthropics/claude-code/tree/main/plugins/ralph-wiggum)
- [Parallel Work Workflow](./../.claude/workflows/parallel-work.md)
- [Autonomous Session Workflow](./../.claude/workflows/autonomous-session.md)
- ADR-0024: Shared Local State Architecture
- ADR-0025: UI-Agnostic Progress Reporting
