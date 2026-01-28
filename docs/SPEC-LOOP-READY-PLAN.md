# Plan: Get PPDS Specs Loop-Ready

## Goal

Fix spec accuracy, finish backend spec gaps, so UI specs can be written on a solid foundation — then ralph/muse builds the UI autonomously.

---

## Phase 1: Fix Drift in Existing Specs — COMPLETE

8 of 12 specs had additive drift (code had more members than specs documented). All fixed on `feat/spec-system-v2`. Staged, not yet committed.

| Spec | What was fixed |
|------|---------------|
| `plugins.md` | +4 properties on `PluginStepAttribute` |
| `analyzers.md` | Status → "Partial (3 of 13 rules implemented)", diagram updated |
| `authentication.md` | `ICredentialProvider` +4 props +1 method, `ISecureCredentialStore` +2 methods, `IGlobalDiscoveryService` removed phantom property, `AuthProfile` expanded from 7 to 20+ properties |
| `connection-pooling.md` | `IDataverseConnectionPool` expanded 3→16 members, `IPooledClient` +5 props, `IConnectionSource` +1 prop, `IThrottleTracker` +5 members |
| `bulk-operations.md` | `BulkOperationOptions` +3 props |
| `cli.md` | `IOutputWriter` +2 methods |
| `query.md` | `QueryResult` +4 props, `IQueryHistoryService` +3 methods |
| `tui.md` | `IHotkeyRegistry` +6 members + `HotkeyBinding` type, `ITuiErrorService` +2 methods |

No drift: `dataverse-services.md`, `mcp.md`, `migration.md`, `architecture.md`.

### Methodology Finding

The spec-gen loop itself caused the drift — LLMs summarize by default, capturing ~70-80% of interface members. A verification phase (PROMPT-SPEC-VERIFY.md) is needed between spec generation and planning. Documented in `vault/Methodology/Specs/ADDENDUM-SPEC-VERIFICATION.md`.

---

## Phase 2: Verify/Finish Backend Spec Gaps — TODO

Four areas need completion before UI specs can reference them accurately.

### 2a. Plugins
- Verify `IPluginRegistrationService` interface in spec matches code
- Verify CLI commands section matches actual `ppds plugins` subcommands
- Check if extract/deploy/diff flows are fully documented
- Source: `src/PPDS.Cli/Plugins/`, `src/PPDS.Cli/Commands/Plugins/`

### 2b. Data Migration
- Verify export/import service interfaces match code
- Check if all import modes documented (upsert, create-only, etc.)
- Verify progress reporting, owner mapping, plugin bypass are covered
- Source: `src/PPDS.Migration/`

### 2c. Plugin Traces
- Verify trace filtering, timeline, settings are in spec
- Check if `IPluginTraceService` interface matches
- Source: `src/PPDS.Dataverse/Services/` (trace-related), `src/PPDS.Cli/Commands/PluginTraces/`

### 2d. Web Resources
- Check if `src/PPDS.Cli/Commands/` has a WebResources directory
- If code exists: write `specs/web-resources.md` (Status: Draft)
- If no code: note for future Draft spec

**Verification discipline:** After updating each spec, count members in spec code blocks vs actual source files. Don't commit until counts match.

---

## Phase 3: Write UI Specs — FUTURE (Human Gate)

Design session in plan mode. Write new specs with `Status: Draft`, `Code: None` for TUI screens/dialogs that consume backend services. Each UI spec references the backend spec it depends on.

This is creative/architectural work — decide which screens to build, what workflows they support, how they compose services. Human reviews and approves before ralph builds.

---

## Phase 4: Run the Loop — FUTURE

1. Generate `IMPLEMENTATION_PLAN.md` from Draft specs using planning prompts
2. Configure muse.toml for ppds (or ralph-win for quick start)
3. Run the loop — human gates at design (spec review) and review (PR merge)

---

## Key Files

| File | Purpose |
|------|---------|
| `ppds/specs/*.md` | 12 specs (all accuracy-fixed in Phase 1) |
| `ppds/specs/SPEC-TEMPLATE.md` | Template for new specs |
| `ppds/docs/SPEC-LOOP-READY-PLAN.md` | This plan |
| `vault/Methodology/Specs/ADDENDUM-SPEC-VERIFICATION.md` | Methodology fix for spec-gen accuracy |
| `vault/Methodology/Specs/PROMPT-SPEC-BUILD.md` | Existing spec-gen prompt |
| `vault/Methodology/Specs/PROMPT-PLAN-*.md` | Planning prompts |
| `vault/Methodology/Specs/PROMPT-BUILD-BUILD.md` | Build loop prompt |

## Git State

- Branch: `feat/spec-system-v2`
- Staged (ppds): 8 modified spec files
- Staged (vault): 1 new addendum file
- Neither committed yet
