# Changelog - PPDS.Migration

All notable changes to PPDS.Migration and PPDS.Migration.Cli will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.0.0] - Unreleased

### Added

#### PPDS.Migration (Library)

- Parallel export with configurable degree of parallelism
- Tiered import with automatic dependency resolution using Tarjan's algorithm
- Circular reference detection with deferred field processing
- CMT format compatibility (schema.xml and data.zip)
- Progress reporting with console and JSON output formats
- Security-first design: connection string redaction, no PII in logs
- DI integration via `AddDataverseMigration()` extension method
- User mapping generation for cross-environment migrations
- Targets: `net8.0`, `net10.0`

#### PPDS.Migration.Cli (CLI Tool)

- Commands: `export`, `import`, `analyze`, `migrate`, `schema generate`, `schema list`, `users generate`
- Global `--url` option for explicit environment URL
- Global `--auth` option with three authentication modes:
  - `interactive` (default): Device code flow with persistent token cache
  - `env`: Environment variables (`DATAVERSE__URL`, `DATAVERSE__CLIENTID`, `DATAVERSE__CLIENTSECRET`)
  - `managed`: Azure Managed Identity for Azure-hosted workloads
- JSON progress output for tool integration (`--json` flag)
- Support for bypass options (plugins, flows) during import
- Validators: file/directory existence checks, numeric range validation
- Upgraded to System.CommandLine 2.0.1 stable
- Packaged as .NET global tool (`ppds-migrate`)
- Targets: `net8.0`, `net10.0`

[1.0.0]: https://github.com/joshsmithxrm/ppds-sdk/releases/tag/Migration-v1.0.0
