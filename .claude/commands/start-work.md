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
Error: No session prompt found.

This command is designed for worktrees created by /plan-work.
Expected file: .claude/session-prompt.md

To create a session prompt:
1. Use /plan-work to triage issues and create worktrees
2. Or manually create .claude/session-prompt.md with your context
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

Based on prompt content, suggest action:

```
Ready to begin. Suggested next steps:
1. Review the issues and acceptance criteria above
2. Explore existing patterns in reference files
3. Start implementing the first item
```

## Output Format

```
================================================================================
WORK SESSION
================================================================================

Branch: feature/phase1-cli-commands
Status: Clean

--------------------------------------------------------------------------------
SESSION PROMPT
--------------------------------------------------------------------------------

[Contents of .claude/session-prompt.md]

--------------------------------------------------------------------------------
READY
--------------------------------------------------------------------------------

Suggested next steps:
1. Review the issues and acceptance criteria above
2. Explore existing patterns in reference files
3. Start implementing the first item
```

## When to Use

- Starting a new Claude Code session in a prepared worktree
- Resuming work after a break
- Onboarding to a worktree created by someone else

## Related Commands

| Command | Purpose |
|---------|---------|
| `/plan-work` | Triage issues, create worktrees with session prompts |
| `/pre-pr` | Validate before creating PR |
| `/handoff` | Generate context summary for next session |
