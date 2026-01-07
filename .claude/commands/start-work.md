# Start Work

Begin a work session in a prepared worktree by reading the session prompt.

## Usage

`/start-work`

No arguments - reads context from the current directory.

## Process

### 1. Check for Session Prompt

Look for `.claude/session-prompt.md` in the current directory.

If not found:
```
No session prompt found at .claude/session-prompt.md

Options:
1. For issue-driven work: Use /plan-work to create worktree with session prompt
2. For ad-hoc work: Use /create-worktree to set up worktree, then enter plan mode
3. Or manually create .claude/session-prompt.md with your context

Tip: Plan mode is the recommended way to establish session context.
     Enter plan mode and describe what you're working on.
```

### 2. Show Branch Context

Display current branch and working directory status:

```bash
git branch --show-current
git status --short
```

Output format:
```
Branch: feature/phase1-cli-commands
Status: Clean (or: 3 uncommitted changes)
```

### 3. Display Session Prompt

Read and display the full contents of `.claude/session-prompt.md`:

```bash
cat .claude/session-prompt.md
```

### 4. Suggest Next Steps

Based on prompt content, suggest entering plan mode:

```
Ready to begin.

Recommended: Enter plan mode to verify and plan implementation.

The session prompt contains a verification checklist that should be
completed before implementing. Plan mode will:
1. Verify current code patterns match issue assumptions
2. Check ADR compliance
3. Create an implementation plan for approval

Enter plan mode now? (or proceed directly with implementation)
```

## Output Format

```
================================================================================
WORK SESSION
================================================================================

Branch: feature/phase1-cli-commands
Status: Clean

--------------------------------------------------------------------------------
SESSION CONTEXT
--------------------------------------------------------------------------------

[Contents of .claude/session-prompt.md]

--------------------------------------------------------------------------------
NEXT STEPS
--------------------------------------------------------------------------------

Recommended: Enter plan mode to verify and plan implementation.

The session context includes:
- Issue details and acceptance criteria
- Verification checklist (patterns, ADRs, staleness)
- Research hints for exploration

Plan mode will help you:
1. Verify the issue's approach against current codebase
2. Create a detailed implementation plan
3. Get approval before making changes

Ready to enter plan mode?
```

## When to Use

- Starting a new Claude Code session in a prepared worktree
- Resuming work after a break
- Onboarding to a worktree created by someone else

## Related Commands

| Command | Purpose |
|---------|---------|
| `/plan-work` | Triage issues, create worktrees with session prompts |
| `/create-worktree` | Create worktree for ad-hoc work (no issue required) |
| `/pre-pr` | Validate before creating PR |
| `/handoff` | Generate context summary for next session |
| `/prune` | Clean up merged worktrees |
