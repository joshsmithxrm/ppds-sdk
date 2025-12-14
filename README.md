# PPDS.Plugins

[![Build](https://github.com/joshsmithxrm/ppds-sdk/actions/workflows/build.yml/badge.svg)](https://github.com/joshsmithxrm/ppds-sdk/actions/workflows/build.yml)
[![NuGet](https://img.shields.io/nuget/v/PPDS.Plugins.svg)](https://www.nuget.org/packages/PPDS.Plugins/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

Plugin development attributes for Microsoft Dataverse. Part of the [Power Platform Developer Suite](https://github.com/joshsmithxrm/power-platform-developer-suite) ecosystem.

## Overview

PPDS.Plugins provides declarative attributes for configuring Dataverse plugin registrations directly in your plugin code. These attributes are extracted by [PPDS.Tools](https://github.com/joshsmithxrm/ppds-tools) to generate registration files that can be deployed to any environment.

## Installation

```bash
dotnet add package PPDS.Plugins
```

Or via the NuGet Package Manager:

```powershell
Install-Package PPDS.Plugins
```

## Usage

### Basic Plugin Step

```csharp
using PPDS.Plugins;

[PluginStep(
    Message = "Create",
    EntityLogicalName = "account",
    Stage = PluginStage.PostOperation)]
public class AccountCreatePlugin : IPlugin
{
    public void Execute(IServiceProvider serviceProvider)
    {
        // Plugin implementation
    }
}
```

### Plugin with Filtering Attributes

```csharp
[PluginStep(
    Message = "Update",
    EntityLogicalName = "contact",
    Stage = PluginStage.PreOperation,
    Mode = PluginMode.Synchronous,
    FilteringAttributes = "firstname,lastname,emailaddress1")]
public class ContactUpdatePlugin : IPlugin
{
    public void Execute(IServiceProvider serviceProvider)
    {
        // Only triggers when specified attributes change
    }
}
```

### Plugin with Images

```csharp
[PluginStep(
    Message = "Update",
    EntityLogicalName = "account",
    Stage = PluginStage.PostOperation)]
[PluginImage(
    ImageType = PluginImageType.PreImage,
    Name = "PreImage",
    Attributes = "name,telephone1,revenue")]
public class AccountAuditPlugin : IPlugin
{
    public void Execute(IServiceProvider serviceProvider)
    {
        // Access pre-image via context.PreEntityImages["PreImage"]
    }
}
```

### Asynchronous Plugin

```csharp
[PluginStep(
    Message = "Create",
    EntityLogicalName = "email",
    Stage = PluginStage.PostOperation,
    Mode = PluginMode.Asynchronous)]
public class EmailNotificationPlugin : IPlugin
{
    public void Execute(IServiceProvider serviceProvider)
    {
        // Runs in background via async service
    }
}
```

## Attributes

### PluginStepAttribute

Defines how a plugin is registered in Dataverse.

| Property | Type | Description |
|----------|------|-------------|
| `Message` | string | SDK message name (Create, Update, Delete, etc.) |
| `EntityLogicalName` | string | Target entity logical name |
| `Stage` | PluginStage | Pipeline stage (PreValidation, PreOperation, PostOperation) |
| `Mode` | PluginMode | Execution mode (Synchronous, Asynchronous) |
| `FilteringAttributes` | string | Comma-separated attributes that trigger the plugin |
| `ExecutionOrder` | int | Order when multiple plugins registered for same event |
| `Name` | string | Display name for the step |
| `StepId` | string | Unique ID for associating images with specific steps |

### PluginImageAttribute

Defines pre/post images for a plugin step.

| Property | Type | Description |
|----------|------|-------------|
| `ImageType` | PluginImageType | PreImage, PostImage, or Both |
| `Name` | string | Key to access image in plugin context |
| `Attributes` | string | Comma-separated attributes to include |
| `EntityAlias` | string | Entity alias (defaults to Name) |
| `StepId` | string | Associates image with specific step |

## Enums

### PluginStage

- `PreValidation (10)` - Before main system validation
- `PreOperation (20)` - Before main operation, within transaction
- `PostOperation (40)` - After main operation

### PluginMode

- `Synchronous (0)` - Immediate execution, blocks operation
- `Asynchronous (1)` - Background execution via async service

### PluginImageType

- `PreImage (0)` - Entity state before operation
- `PostImage (1)` - Entity state after operation
- `Both (2)` - Both pre and post images

## Related Projects

- [power-platform-developer-suite](https://github.com/joshsmithxrm/power-platform-developer-suite) - VS Code extension
- [ppds-tools](https://github.com/joshsmithxrm/ppds-tools) - PowerShell deployment module
- [ppds-alm](https://github.com/joshsmithxrm/ppds-alm) - CI/CD pipeline templates
- [ppds-demo](https://github.com/joshsmithxrm/ppds-demo) - Reference implementation

## License

MIT License - see [LICENSE](LICENSE) for details.
