# Changelog - PPDS.Dataverse

All notable changes to PPDS.Dataverse will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added

- Per-connection pool sizing (`MaxConnectionsPerUser`) for optimal throughput
- AIMD-based adaptive rate control for throttle recovery
- Structured configuration with typed auth properties
- Key Vault and environment variable secret resolution
- Multi-environment configuration support

## [1.0.0] - TBD

### Added

- Multi-connection pool supporting multiple Application Users for load distribution
- Connection selection strategies: RoundRobin, LeastConnections, ThrottleAware
- Throttle tracking with automatic routing away from throttled connections
- Bulk operation wrappers: CreateMultiple, UpdateMultiple, UpsertMultiple, DeleteMultiple
- `IProgress<ProgressSnapshot>` support for real-time progress reporting during bulk operations
- DI integration via `AddDataverseConnectionPool()` extension method
- Affinity cookie disabled by default for improved throughput
- TVP race condition retry (SQL error 3732)
- SQL deadlock retry (SQL error 1205)
- Connection validation with background health checks
- Security-first design: connection string redaction, sensitive data attributes
- Targets: `net8.0`, `net10.0`

[Unreleased]: https://github.com/joshsmithxrm/ppds-sdk/compare/Dataverse-v1.0.0...HEAD
[1.0.0]: https://github.com/joshsmithxrm/ppds-sdk/releases/tag/Dataverse-v1.0.0
