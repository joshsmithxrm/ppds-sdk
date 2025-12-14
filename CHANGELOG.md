# Changelog

All notable changes to PPDS.Plugins will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [1.0.0] - 2025-01-XX

### Added
- `PluginStepAttribute` for declarative plugin step registration
- `PluginImageAttribute` for defining pre/post images
- `PluginStage` enum (PreValidation, PreOperation, PostOperation)
- `PluginMode` enum (Synchronous, Asynchronous)
- `PluginImageType` enum (PreImage, PostImage, Both)
- Multi-targeting: net462, net6.0, net8.0
- Strong name signing for Dataverse compatibility
- Full XML documentation
- GitHub Actions workflows for build and NuGet publishing

[Unreleased]: https://github.com/joshsmithxrm/ppds-sdk/compare/v1.0.0...HEAD
[1.0.0]: https://github.com/joshsmithxrm/ppds-sdk/releases/tag/v1.0.0
