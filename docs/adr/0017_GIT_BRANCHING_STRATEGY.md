# ADR-0017: Git Branching Strategy

**Status:** Accepted
**Date:** 2026-01-07
**Authors:** Josh, Claude

## Context

The ppds-sdk repository uses git worktrees for parallel development. Without documented conventions:
- Branch names are inconsistent
- Worktree locations vary
- Unclear when to create worktrees vs branches
- PR workflow not standardized

## Decision

### Branch Naming

| Prefix | Purpose | Example |
|--------|---------|---------|
| `feature/` | New functionality | `feature/plugin-traces` |
| `fix/` | Bug fixes | `fix/bypass-plugins` |
| `docs/` | Documentation only | `docs/file-format-policy` |
| `chore/` | Maintenance, refactoring | `chore/pre-pr-cleanup` |
| `release/` | Release preparation | `release/v1.0.0` |

### Worktree Strategy

Use worktrees for parallel, isolated work. Each worktree is a separate directory with its own branch.

**Location:** `C:\VS\ppds\sdk-{branch-suffix}`

```
C:\VS\ppds\
├── sdk/                    # Main repo (main or current work)
├── sdk-plugin-traces/      # feature/plugin-traces
├── sdk-tui-enhancements/   # feature/tui-enhancements
└── sdk-file-format-adr/    # docs/file-format-policy
```

**When to use worktrees:**
- Work spans multiple sessions
- Need to context-switch between features
- Large feature requiring isolation

**When to use simple branches:**
- Quick fixes (< 1 hour)
- Single-session work
- No need to switch away

### Worktree Lifecycle

```bash
# Create worktree from main
git worktree add ../sdk-{name} main
cd ../sdk-{name}
git checkout -b feature/{name}

# Work, commit, push
git push -u origin feature/{name}
gh pr create

# After PR merges, clean up
git worktree remove ../sdk-{name}
```

### Main Branch Protection

- `main` is protected; direct commits blocked
- All changes require PR with passing CI
- Squash merge preferred for clean history

### PR Conventions

1. **Title:** `type: description` (e.g., `feat: add plugin traces CLI`)
2. **Body:** Include "Closes #123" for linked issues
3. **CI:** All checks must pass
4. **Review:** Required for non-docs changes

## Consequences

### Positive
- Consistent branch naming aids navigation
- Worktrees enable true parallel development
- Clear lifecycle prevents orphaned worktrees
- Protected main ensures stability

### Negative
- More disk space for multiple worktrees
- Must remember to clean up after merge

### Neutral
- Existing workflows unchanged
- Optional adoption of worktree pattern
