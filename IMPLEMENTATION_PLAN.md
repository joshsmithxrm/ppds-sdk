# Specification Generation Plan

**Repository:** ppds
**Created:** 2026-01-27
**Status:** Ready for Approval

---

## Summary

| Metric | Count |
|--------|-------|
| Projects explored | 7 |
| Existing specs | 10 |
| Missing specs (declared in README) | 2 |
| New specs to generate | 2 |
| Specs needing expansion | 1 |

---

## Tasks

### Spec Generation

- [x] **Generate spec: plugins.md** <!-- id: spec-plugins -->
    - Source: `src/PPDS.Plugins/` + `src/PPDS.Cli/Plugins/`
    - Priority: P1 (declared in README but missing)
    - Content: Plugin attributes, enums, extraction, registration workflow

- [x] **Generate spec: analyzers.md** <!-- id: spec-analyzers -->
    - Source: `src/PPDS.Analyzers/`
    - Priority: P1 (declared in README but missing)
    - Content: Roslyn analyzer rules, diagnostic codes (PPDS001-PPDS013)

### Spec Expansion

- [ ] **Expand cli.md: Add CsvLoader section** <!-- id: expand-cli-csvloader -->
    - Source: `src/PPDS.Cli/CsvLoader/` (14 files)
    - Priority: P2
    - Content: Data loading, auto-mapping, lookup resolution, mapping files
    - Rationale: Substantial subsystem (14 files) not documented anywhere

---

## Exploration Evidence

### Project Inventory

| Project | Subdirectory | Files | Key Interfaces | Notes |
|---------|--------------|-------|----------------|-------|
| PPDS.Cli | Commands/ | ~45 | - | Covered by cli.md |
| PPDS.Cli | Infrastructure/ | ~15 | IOutputWriter, IRpcLogger | Covered by cli.md |
| PPDS.Cli | Services/ | ~10 | ISqlQueryService, IProfileService | Covered by architecture.md |
| PPDS.Cli | CsvLoader/ | 14 | - | **NOT COVERED** |
| PPDS.Cli | Plugins/ | 5 | IPluginRegistrationService | Needs plugins.md |
| PPDS.Cli | Tui/ | ~35 | ITuiStateCapture | Covered by tui.md |
| PPDS.Dataverse | Pooling/ | 12 | IDataverseConnectionPool | Covered by connection-pooling.md |
| PPDS.Dataverse | Services/ | 15 | 10 domain service interfaces | Covered by dataverse-services.md |
| PPDS.Dataverse | Sql/ | 14 | ISqlCondition | Covered by query.md |
| PPDS.Dataverse | BulkOperations/ | 7 | IBulkOperationExecutor | Covered by bulk-operations.md |
| PPDS.Auth | Credentials/ | 21 | ICredentialProvider | Covered by authentication.md |
| PPDS.Auth | Profiles/ | 9 | - | Covered by authentication.md |
| PPDS.Migration | Import/ | 18 | IImporter, IImportPhaseProcessor | Covered by migration.md |
| PPDS.Migration | Formats/ | 9 | ICmtDataReader/Writer | Covered by migration.md |
| PPDS.Mcp | Tools/ | 13 | - | Covered by mcp.md |
| PPDS.Plugins | Attributes/ | 2 | - | **NEEDS SPEC** |
| PPDS.Plugins | Enums/ | 3 | - | **NEEDS SPEC** |
| PPDS.Analyzers | Rules/ | 3 | - | **NEEDS SPEC** |

### Significance Matrix

| Subdirectory | Files | Interface? | Verdict | Proof |
|--------------|-------|------------|---------|-------|
| PPDS.Cli/Commands/ | ~45 | No | COVERED | cli.md § Command Groups |
| PPDS.Cli/CsvLoader/ | 14 | No | NEEDS EXPANSION | cli.md mentions `data load` but no section |
| PPDS.Cli/Plugins/ | 5 | Yes | SPEC NEEDED | Not in any spec |
| PPDS.Cli/Services/ | ~10 | Yes | COVERED | architecture.md § Application Services |
| PPDS.Cli/Tui/ | ~35 | Yes | COVERED | tui.md (dedicated spec) |
| PPDS.Dataverse/Pooling/ | 12 | Yes | COVERED | connection-pooling.md (dedicated spec) |
| PPDS.Dataverse/Services/ | 15 | Yes | COVERED | dataverse-services.md (dedicated spec) |
| PPDS.Dataverse/Sql/ | 14 | Yes | COVERED | query.md § SQL Parser |
| PPDS.Auth/ | 35 | Yes | COVERED | authentication.md (dedicated spec) |
| PPDS.Migration/ | 45 | Yes | COVERED | migration.md (dedicated spec) |
| PPDS.Mcp/ | 17 | Yes | COVERED | mcp.md (dedicated spec) |
| PPDS.Plugins/ | 5 | No | SPEC NEEDED | Declared in README, file missing |
| PPDS.Analyzers/ | 4 | No | SPEC NEEDED | Declared in README, file missing |

### Existing Specs Audit

| Spec | Issue | Remediation |
|------|-------|-------------|
| README.md | Lists plugins.md but file doesn't exist | Generate plugins.md |
| README.md | Lists analyzers.md but file doesn't exist | Generate analyzers.md |
| cli.md | CsvLoader not documented | Add § Data Loading section |

---

## Critical Files

### For plugins.md
- `src/PPDS.Plugins/Attributes/PluginStepAttribute.cs` - Core attribute contract
- `src/PPDS.Plugins/Attributes/PluginImageAttribute.cs` - Image attribute
- `src/PPDS.Plugins/Enums/PluginStage.cs` - Stage enum
- `src/PPDS.Cli/Plugins/Extraction/AssemblyExtractor.cs` - Reads attributes
- `src/PPDS.Cli/Plugins/Registration/PluginRegistrationService.cs` - Registers plugins

### For analyzers.md
- `src/PPDS.Analyzers/DiagnosticIds.cs` - All 13 diagnostic codes
- `src/PPDS.Analyzers/Rules/NoFireAndForgetInCtorAnalyzer.cs`
- `src/PPDS.Analyzers/Rules/NoSyncOverAsyncAnalyzer.cs`
- `src/PPDS.Analyzers/Rules/UseEarlyBoundEntitiesAnalyzer.cs`

### For cli.md expansion
- `src/PPDS.Cli/CsvLoader/CsvDataLoader.cs` - Main loader (807 lines)
- `src/PPDS.Cli/CsvLoader/ColumnMatcher.cs` - Auto-mapping logic
- `src/PPDS.Cli/CsvLoader/MappingGenerator.cs` - Config generation

---

## Verification

After generation, verify:
1. All specs follow SPEC-TEMPLATE.md structure
2. Cross-references are bidirectional (Related Specs sections)
3. README.md links work for new specs
4. Code paths in spec headers are accurate
