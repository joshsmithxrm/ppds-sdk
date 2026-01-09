# UI Development Workflow

Reference-driven design for TUI and VS Code extension.

## The Problem

Building "beautiful" UIs with Claude leads to 40+ iteration sessions because Claude guesses what "beautiful" means to you. Each iteration is Claude learning your taste through trial and error.

## The Solution

Invest upfront in wireframes and references, so implementation is 1-shot.

```
/design-ui "import job monitor"
    ↓
Claude: "Show me 1-2 reference UIs"
    ↓
You: "Like lazydocker's container list"
    ↓
Claude creates ASCII wireframe
    ↓
You iterate on wireframe (fast, low-cost)
    ↓
Claude writes spec to issue (wireframe + reference + behavior)
    ↓
Implementation is 1-shot
```

**Key insight:** Iterate on the wireframe, not the implementation. Wireframe iteration is 10x faster.

## Reference Apps

| App | What to Steal |
|-----|---------------|
| lazydocker | List → details → logs pattern, panel layout |
| k9s | Table styling, keyboard shortcuts, action hints |
| lazygit | Color scheme, panel transitions, modal dialogs |
| visidata | Table rendering, column sizing, filtering |

## Design System

### Colors

| Element | Color | Usage |
|---------|-------|-------|
| Production env | Red | Always visible, danger zone |
| UAT/Staging | Yellow | Caution |
| Sandbox | Green | Safe to experiment |
| Dev | Blue | Development work |

### Splash Screen

- Hacker aesthetic (Matrix-y, cyber, glitchy text effects)
- Quick flash (1-2 seconds)
- ASCII art + version
- "We didn't have to do this but we did"

### Core Primitives

Build in this order:

| Order | Component | Scope |
|-------|-----------|-------|
| 1 | Data Table | Filter, sort, cell select, drill to detail |
| 2 | Detail Pane | Tabs, raw JSON, "not hiding anything" |
| 3 | Environment Indicator | Always visible, configurable |
| 4 | Tree View | Hierarchical with filtering |
| 5 | Command Palette | Quick jump, keyboard-driven |
| 6 | Diff/Compare View | Side-by-side with highlighting |
| 7 | Timeline/Trace View | Temporal data, find problems fast |

## Architecture

Views are dumb renderers. Logic is shared.

```
┌─────────────────────────────────────────────────────┐
│               Application Services                   │
│  IImportJobService, ISolutionService, etc.          │
├─────────────────────────────────────────────────────┤
│    TUI Views       │   VS Code Views    │   CLI     │
│  (Terminal.Gui)    │   (Webview/Tree)   │  (table)  │
└─────────────────────────────────────────────────────┘
```

Same component, different renderer:
- "Import Job List" → TUI table, VS Code tree view
- "Job Details" → TUI panel, VS Code webview
- "Progress Monitor" → TUI status bar, VS Code notification

## Component Build Process

For each component:

1. **Design with references**
   - `/design-ui "component name"`
   - Provide reference apps
   - Create ASCII wireframe
   - Iterate until approved

2. **Implement TUI first**
   - Build with proper abstraction
   - Test with `/tui-test`
   - Document learnings

3. **Implement VS Code**
   - Use same abstraction
   - Port TUI patterns

4. **Update design system**
   - Document what worked
   - Extract reusable patterns

## Multi-Environment UX

### TUI (V1)
- Single environment per TUI instance
- Multiple terminal tabs = multiple environments
- Prominent status bar indicator
- Splash screen confirms environment on launch

### VS Code
1. **Tab-per-environment** - Each panel has environment tabs
2. **Compare mode** - Purpose-built comparison with diff highlighting

## Wireframe Example

```
┌─ Import Jobs ───────────────────┬─ Details ─────────────────────┐
│ Filter: [____________] [Go]     │ Job: Contoso Data Load        │
├─────────────────────────────────│                               │
│ [>] Contoso Data Load    ✓ Done │ Status: Completed             │
│     Account Migration    ⏳ 45% │ Records: 1,234 / 1,234        │
│     Contact Import       ✗ Fail │ Duration: 2m 34s              │
│                                 ├───────────────────────────────┤
│                                 │ [Summary] [Errors] [Raw JSON] │
│                                 │                               │
│                                 │ All records imported.         │
│                                 │ No errors.                    │
├─────────────────────────────────┴───────────────────────────────┤
│ 🟢 DEV | Connected | ↑↓ Navigate | Enter: Select | ?: Help      │
└─────────────────────────────────────────────────────────────────┘
```

## Related

- [Human Gates](./human-gates.md) - Design approval gate
- `/design-ui` command - Reference-driven UI design
