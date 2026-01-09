# Human Gates

When Claude should stop and ask for human input.

## The Principle

Claude works autonomously on implementation but defers to human expertise for:
- **Design decisions** - What to build and how
- **Priority decisions** - What matters, what's strategic
- **Domain-specific gotchas** - "That won't work because Dataverse does X"
- **Final quality gate** - PR review before merge

## Gate Types

### 1. Design Gate

**When:** Starting new feature or significant change

**Trigger:** User invokes `/design` or `/design-ui`

**Process:**
1. Claude explores codebase, gathers context
2. Claude proposes approach with options
3. Human approves or redirects
4. Claude proceeds with approved design

### 2. Planning Gate

**When:** Distributing work across parallel sessions

**Trigger:** User invokes `/plan-work`

**Process:**
1. Claude analyzes project state
2. Claude proposes N workstreams with issue groupings
3. Human approves or adjusts
4. Claude spawns sessions

### 3. Domain Gates (During Implementation)

**When:** Implementation touches sensitive areas

**Triggers:**
| Area | Examples |
|------|----------|
| Auth/Security | Token handling, credential storage, permission checks |
| Performance-critical | Bulk operations, connection pooling, parallelism values |

**Process:**
1. Claude identifies gate trigger
2. Updates session status to `stuck`
3. Includes context: what decision is needed, options considered
4. Waits for orchestrator to relay guidance
5. Proceeds with approved approach

### 4. Stuck Gate

**When:** Claude can't make progress

**Triggers:**
- Same test failure 3 times
- CI failure after 3 fix attempts
- Unclear requirements
- Missing dependencies or access

**Process:**
1. Claude updates session status to `stuck`
2. Includes: what's blocking, what was tried, what's needed
3. Waits for guidance or resolution
4. Continues after blocker resolved

### 5. PR Review Gate

**When:** Work complete, PR created

**Trigger:** `/ship` completes successfully

**Process:**
1. Claude creates PR, handles CI and bot comments autonomously
2. Updates session status to `pr_ready`
3. Human reviews PR
4. Human merges (Claude never merges)

## Escalation Method

Claude uses **collect and batch** for escalations:
- Note all questions/blockers during work
- Ask once at natural pause points (end of exploration, before major change)
- Don't interrupt for every small decision

## What Claude Handles Autonomously

| Activity | Autonomous? |
|----------|-------------|
| Implementation (once design approved) | Yes |
| Test-fix loops | Yes (until stuck) |
| Pre-PR validation | Yes |
| CI failure diagnosis and fix | Yes (up to 3 attempts) |
| Bot comment resolution | Yes |
| Branch/worktree management | Yes |
| Documentation updates | Yes |

## What Requires Human

| Activity | Why Human? |
|----------|------------|
| What to build | Strategic, domain expertise |
| How to prioritize | Business value, timing |
| Auth/Security approach | Security-critical |
| Performance tradeoffs | Domain-specific knowledge |
| PR merge | Final quality gate |
| Release decisions | Timing, coordination |

## Related

- [Autonomous Session](./autonomous-session.md) - Session flow
- [Parallel Work](./parallel-work.md) - Orchestrator interaction
