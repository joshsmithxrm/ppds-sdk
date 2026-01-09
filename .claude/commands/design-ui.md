# Design UI

Reference-driven UI design for TUI and VS Code extension.

## Usage

`/design-ui [component-name]`

Examples:
- `/design-ui import-job-monitor` - Design the import job monitoring screen
- `/design-ui plugin-tree` - Design the plugin registration tree view
- `/design-ui data-table` - Design the shared data table component

## Arguments

`$ARGUMENTS` - Component or screen name (optional, will prompt)

## The Problem This Solves

Building "beautiful" UIs leads to 40+ iteration sessions because Claude guesses what "beautiful" means. Each iteration is learning your taste through trial and error.

**Solution:** Invest upfront in wireframes and references, so implementation is 1-shot.

## Process

### 1. Gather References

Ask the user for reference UIs:

```
What UI should this feel like?

Suggested references for PPDS:
- lazydocker (list → details → logs)
- k9s (tables, actions, keyboard hints)
- lazygit (color scheme, panel transitions)
- visidata (table rendering, filtering)

Show me 1-2 references, or describe the vibe you want.
```

**STOP and wait for user input.**

### 2. Create ASCII Wireframe

Based on references, create an ASCII wireframe:

```
┌─ [Component Name] ──────────────┬─ Details ─────────────────────┐
│ Filter: [____________] [Go]     │ Title: Selected Item          │
├─────────────────────────────────│                               │
│ [>] Item 1              ✓ Done  │ Field: Value                  │
│     Item 2              ⏳ 45%  │ Field: Value                  │
│     Item 3              ✗ Fail  │ Field: Value                  │
│                                 ├───────────────────────────────┤
│                                 │ [Tab 1] [Tab 2] [Raw JSON]    │
│                                 │                               │
│                                 │ Content for selected tab      │
├─────────────────────────────────┴───────────────────────────────┤
│ 🟢 DEV | Status info | Keyboard hints                          │
└─────────────────────────────────────────────────────────────────┘
```

Present to user and ask for feedback.

### 3. Iterate on Wireframe

Refine based on feedback:
- Layout adjustments
- Panel proportions
- Information to display
- Keyboard shortcuts
- Status bar content

**Key insight:** Iterate here, not during implementation. Wireframe changes are fast.

### 4. Define Behavior

Document keyboard navigation and interactions:

```
## Keyboard Navigation

| Key | Action |
|-----|--------|
| ↑/↓ | Navigate list |
| Enter | Select item, show details |
| Tab | Cycle through panes |
| / | Open filter |
| ? | Show help |
| q | Quit |

## Interactions

- Selecting item updates detail pane
- Filter narrows list in real-time
- Raw JSON tab shows unformatted data
```

### 5. Write Spec to Issue

Create a GitHub issue with the complete spec:

```markdown
## UI: [Component Name]

### Reference
Like [reference app]'s [specific screen/pattern]

### Wireframe
```
[ASCII wireframe]
```

### Keyboard Navigation
[table from step 4]

### Behavior
- [interaction 1]
- [interaction 2]

### Acceptance Criteria
- [ ] Layout matches wireframe
- [ ] All keyboard shortcuts work
- [ ] Status bar shows environment
- [ ] Raw JSON tab available
```

### 6. Create for Both Platforms

If the component needs both TUI and VS Code:

**TUI wireframe** (ASCII, Terminal.Gui)
**VS Code wireframe** (describe webview or tree structure)

Note differences:
- VS Code may use native tree views instead of custom tables
- VS Code has more color options
- TUI is keyboard-only, VS Code has mouse

## Design System Reference

### Colors

| Element | Color |
|---------|-------|
| Production | Red (danger) |
| UAT/Staging | Yellow (caution) |
| Sandbox | Green (safe) |
| Dev | Blue (development) |

### Core Patterns

- **List/Detail split**: Left list, right detail pane
- **Status bar**: Always visible, shows environment + hints
- **Raw JSON tab**: "Not hiding anything" philosophy
- **Keyboard-first**: Every action has a shortcut

### Splash Screen

- Hacker aesthetic
- Quick flash (1-2 seconds)
- ASCII art + version
- "We didn't have to do this but we did"

## Output

After completing design:

```
UI Design Complete
==================

Component: [name]
Issue: #[number] created with full spec

Files created:
- .claude/design-ui.md (wireframes, behavior)

Next steps:
1. Review issue spec
2. Implement TUI version first
3. Port to VS Code after TUI works

See: .claude/workflows/ui-development.md
```

## When to Use

- Before building any new TUI screen
- Before adding VS Code extension views
- When redesigning existing UI
- For shared components (data table, detail pane)

## Related

- [UI Development Workflow](.claude/workflows/ui-development.md)
- `/design` - General feature design
- `/test --tui` - Test TUI implementation
