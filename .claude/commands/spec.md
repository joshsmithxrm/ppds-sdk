# Spec

Generate a contributor-ready implementation guide from a triaged issue.

## Usage

`/spec <issue-number>`

Examples:
- `/spec 123` - Generate implementation spec for issue #123
- `/spec 456` - Create contributor guide for issue #456

## Arguments

`$ARGUMENTS` - GitHub issue number (required)

## Purpose

Transforms a triaged issue into a detailed implementation guide that enables external contributors to implement the feature without deep codebase knowledge.

## Process

### 1. Fetch Issue

```bash
gh issue view <number> --json number,title,body,labels,milestone
```

Verify the issue is triaged:
- Has Type label (feature, bug, enhancement)
- Has Priority label (P0-P3)
- Has Size estimate (XS-XL)

If not triaged, prompt to run `/triage` first.

### 2. Analyze Codebase

Explore to understand:
- Existing patterns for similar features
- Files that will need modification
- Service interfaces involved
- Test patterns to follow

### 3. Generate Spec

Create implementation guide:

```markdown
## Implementation Spec: [Issue Title]

**Issue:** #[number]
**Size:** [XS/S/M/L/XL]
**Difficulty:** [Beginner/Intermediate/Advanced]

### Overview
[1-2 sentences explaining what needs to be built]

### Prerequisites
- Familiarity with [technologies]
- Understanding of [patterns]

### Implementation Steps

#### Step 1: [First Task]
**Files:** `path/to/file.cs`

[Detailed instructions]

```csharp
// Example code snippet showing the pattern
public interface IExampleService
{
    Task<Result> DoSomethingAsync();
}
```

#### Step 2: [Second Task]
...

### Testing Requirements

1. **Unit Tests**
   - Test file: `tests/Project.Tests/[Name]Tests.cs`
   - Must cover: [scenarios]

2. **Integration Tests** (if applicable)
   - Requires: [credentials/setup]
   - Test: [what to verify]

### Definition of Done

- [ ] Implementation follows existing patterns
- [ ] Unit tests pass with meaningful coverage
- [ ] XML documentation on public APIs
- [ ] CHANGELOG.md updated
- [ ] No compiler warnings

### Reference Files

| Purpose | Path |
|---------|------|
| Similar feature | `src/...` |
| Service pattern | `src/...` |
| Test example | `tests/...` |

### Common Pitfalls

- [Gotcha 1]
- [Gotcha 2]

### Questions?

If you get stuck:
1. Check existing implementations in [paths]
2. Review ADRs in `docs/adr/`
3. Open a draft PR and ask for guidance
```

### 4. Update Issue

Add the spec as a comment on the issue and add `good first issue` label if appropriate:

```bash
gh issue comment <number> --body "<spec content>"
gh issue edit <number> --add-label "good first issue"
```

## Difficulty Assessment

| Difficulty | Criteria |
|------------|----------|
| Beginner | Single file, follows existing pattern exactly |
| Intermediate | Multiple files, some design decisions needed |
| Advanced | Cross-cutting, architectural implications |

Add `good first issue` label only for Beginner difficulty.

## Output

```
Spec Generated
==============

Issue: #123 - Add retry logic to bulk operations
Size: M
Difficulty: Intermediate

Spec added as comment on issue.
Labels updated: good first issue (if applicable)

Issue URL: https://github.com/.../issues/123
```

## When to Use

- After triaging issues to make them contributor-ready
- When external contributor expresses interest
- For issues marked as potential community contributions
- To document implementation approach for complex issues

## What Makes a Good Spec

1. **Self-contained** - Contributor doesn't need to ask questions
2. **Specific files** - Exact paths, not "somewhere in src"
3. **Code examples** - Show the pattern, not just describe it
4. **Test requirements** - Clear on what tests are needed
5. **Pitfalls** - Common mistakes to avoid

## Related Commands

| Command | Purpose |
|---------|---------|
| `/triage` | Triage issues before generating specs |
| `/create-issue` | Create new issues |
| `/design` | Full design session for complex features |
