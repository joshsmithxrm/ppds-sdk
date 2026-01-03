# Code Coverage Baseline

Established: January 2026

## Overview

This document records the baseline code coverage for each PPDS SDK package, used to track progress toward the targets defined in [Issue #55](https://github.com/joshsmithxrm/ppds-sdk/issues/55).

## Current Baseline

| Package | Baseline | Target | Gap | Notes |
|---------|----------|--------|-----|-------|
| **PPDS.Plugins** | 95% | 95% | ✅ Met | Core attributes fully tested |
| **PPDS.Dataverse** | 62% | 60% | ✅ Met | Unit + FakeXrmEasy mocked tests |
| **PPDS.Cli** | 12% | 60% | -48% | Commands need E2E tests |
| **PPDS.Auth** | 0% | 70% | -70% | No unit test project yet |
| **PPDS.Migration** | 0% | 50% | -50% | No unit test project yet |
| **Overall** | ~38% | 60% | -22% | Weighted by lines of code |

## Measurement Method

Coverage is collected using:
- **Collector**: `coverlet.collector` via `dotnet test --collect:"XPlat Code Coverage"`
- **Reporter**: [Codecov](https://codecov.io/gh/joshsmithxrm/ppds-sdk)
- **Frameworks**: Merged from net8.0, net9.0, net10.0 test runs

## Targets by Package

From [Issue #55 - Integration Testing Infrastructure](https://github.com/joshsmithxrm/ppds-sdk/issues/55):

| Package | Unit Target | Integration Target | Rationale |
|---------|-------------|-------------------|-----------|
| PPDS.Plugins | 95% | N/A | Core attributes, simple logic |
| PPDS.Auth | 70% | Live auth tests | Complex auth flows |
| PPDS.Dataverse | 60% | 80% mocked | Heavy external dependencies |
| PPDS.Migration | 50% | N/A | Orchestration code |
| PPDS.Cli | 60% | E2E tests | Command parsing + wiring |

## Improvement Plan

### Phase 5 (Current)
- [ ] Create PPDS.Auth.Tests unit test project
- [ ] Create PPDS.Migration.Tests unit test project
- [ ] Add CLI E2E tests to PPDS.LiveTests

### Future
- [ ] Enable patch coverage enforcement (80% for new code)
- [ ] Add project-level coverage gates after baseline improvement

## Codecov Configuration

Coverage thresholds are configured in [`codecov.yml`](../codecov.yml):
- **Project coverage**: Informational (no PR blocking)
- **Patch coverage**: Informational (no PR blocking)
- **Components**: Per-package tracking with individual targets

## References

- [Codecov Dashboard](https://codecov.io/gh/joshsmithxrm/ppds-sdk)
- [Issue #55 - Integration Testing Infrastructure](https://github.com/joshsmithxrm/ppds-sdk/issues/55)
- [Issue #84 - Code Coverage Reporting](https://github.com/joshsmithxrm/ppds-sdk/issues/84)
