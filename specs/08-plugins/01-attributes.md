# PPDS.Plugins: Attributes

## Overview

The PPDS.Plugins Attributes subsystem provides a declarative, attribute-based system for Dataverse plugin registration. Developers specify plugin configuration using C# attributes applied to plugin classes, which are extracted at runtime by the CLI and converted to Dataverse plugin registration metadata. This co-locates registration configuration with plugin implementation for improved maintainability.

## Public API

### Attributes

| Attribute | Purpose | Target |
|-----------|---------|--------|
| `PluginStepAttribute` | Defines plugin step registration | Class (multiple allowed) |
| `PluginImageAttribute` | Defines pre/post-image snapshots | Class (multiple allowed) |

### Enums

| Enum | Purpose |
|------|---------|
| `PluginStage` | Pipeline execution stage |
| `PluginMode` | Sync/async execution mode |
| `PluginImageType` | Pre-image, post-image, or both |

## PluginStepAttribute

Defines plugin step registration configuration.

### Properties

| Property | Type | Required | Default | Description |
|----------|------|----------|---------|-------------|
| `Message` | string | Yes | "" | SDK message (Create, Update, Delete, etc.) |
| `EntityLogicalName` | string | Yes | "" | Primary entity; "none" for global messages |
| `SecondaryEntityLogicalName` | string? | No | null | Secondary entity (Associate, etc.) |
| `Stage` | PluginStage | Yes | - | PreValidation, PreOperation, PostOperation |
| `Mode` | PluginMode | No | Synchronous | Synchronous or Asynchronous |
| `FilteringAttributes` | string? | No | null | Comma-separated trigger attributes |
| `ExecutionOrder` | int | No | 1 | Execution sequence (1-999999) |
| `Name` | string? | No | null | Display name (auto-generated if null) |
| `UnsecureConfiguration` | string? | No | null | Plugin constructor configuration |
| `Description` | string? | No | null | Step documentation |
| `AsyncAutoDelete` | bool | No | false | Auto-delete async job on success |
| `StepId` | string? | No | null | Links images to specific step |

### Constructors

```csharp
// Parameterless (all properties use defaults)
public PluginStepAttribute()

// Required properties
public PluginStepAttribute(string message, string entityLogicalName, PluginStage stage)
```

### Usage

```csharp
[PluginStep(
    Message = "Update",
    EntityLogicalName = "account",
    Stage = PluginStage.PostOperation,
    Mode = PluginMode.Asynchronous,
    FilteringAttributes = "name,telephone1",
    ExecutionOrder = 10,
    Description = "Logs account changes to audit table",
    AsyncAutoDelete = true)]
public class AccountUpdatePlugin : IPlugin
{
    public void Execute(IServiceProvider serviceProvider) { }
}
```

### Auto-Generated Name

If `Name` is not specified, it is generated as:
```
{TypeName}: {Message} of {EntityLogicalName}
```

Example: `AccountUpdatePlugin: Update of account`

## PluginImageAttribute

Defines pre-image or post-image snapshots for a plugin step.

### Properties

| Property | Type | Required | Default | Description |
|----------|------|----------|---------|-------------|
| `ImageType` | PluginImageType | Yes | - | PreImage, PostImage, or Both |
| `Name` | string | Yes | "" | Key in context.PreEntityImages/PostEntityImages |
| `Attributes` | string? | No | null | Comma-separated attributes; null = all |
| `EntityAlias` | string? | No | null | Entity alias (defaults to Name) |
| `StepId` | string? | No | null | Associates with specific step |

### Constructors

```csharp
// Parameterless
public PluginImageAttribute()

// Required properties
public PluginImageAttribute(PluginImageType imageType, string name)

// Common usage with attributes
public PluginImageAttribute(PluginImageType imageType, string name, string attributes)
```

### Usage

```csharp
[PluginStep("Update", "account", PluginStage.PostOperation)]
[PluginImage(
    ImageType = PluginImageType.PreImage,
    Name = "PreImage",
    Attributes = "name,telephone1,revenue")]
public class AccountAuditPlugin : IPlugin
{
    public void Execute(IServiceProvider serviceProvider)
    {
        var context = (IPluginExecutionContext)serviceProvider
            .GetService(typeof(IPluginExecutionContext));
        var preImage = context.PreEntityImages["PreImage"];
    }
}
```

## Enums

### PluginStage

```csharp
public enum PluginStage
{
    PreValidation = 10,  // Before system validation
    PreOperation = 20,   // Before database operation
    PostOperation = 40   // After database operation
}
```

### PluginMode

```csharp
public enum PluginMode
{
    Synchronous = 0,   // Blocks the operation
    Asynchronous = 1   // Background execution
}
```

### PluginImageType

```csharp
public enum PluginImageType
{
    PreImage = 0,   // Snapshot before operation
    PostImage = 1,  // Snapshot after operation
    Both = 2        // Both snapshots (PostOperation only)
}
```

## Behaviors

### Image Availability by Stage

| Stage | PreImage | PostImage | Both |
|-------|----------|-----------|------|
| PreValidation | No | No | No |
| PreOperation | Yes | No | No |
| PostOperation | Yes | Yes | Yes |

### Multi-Step Plugins

For plugins handling multiple events, use `StepId` to link images to specific steps:

```csharp
[PluginStep("Create", "account", PluginStage.PostOperation, StepId = "createStep")]
[PluginStep("Update", "account", PluginStage.PostOperation, StepId = "updateStep")]
[PluginImage(PluginImageType.PreImage, "Pre", "name,revenue", StepId = "updateStep")]
[PluginImage(PluginImageType.PostImage, "Post", "name,revenue", StepId = "updateStep")]
public class AccountPlugin : IPlugin { }
```

- `StepId` on image matches `StepId` on step for association
- Image without `StepId` applies to all steps
- Create step: no images
- Update step: has pre and post images

### Attribute Extraction

Attributes are extracted at CLI execution time via:

1. **MetadataLoadContext** - Safe assembly loading
2. **CustomAttributeData** - Reflection-safe attribute inspection
3. **AssemblyExtractor** - Scans all public types for attributes
4. **Type filtering** - Skips abstract types and interfaces

## Edge Cases

| Scenario | Behavior | Notes |
|----------|----------|-------|
| No Name specified | Auto-generated | `{TypeName}: {Message} of {Entity}` |
| No EntityAlias | Defaults to Name | Image access key |
| No Attributes on image | All attributes included | Not recommended for performance |
| Abstract class with attribute | Skipped | Only concrete types processed |
| Image without StepId | Applies to all steps | Default association |
| ExecutionOrder out of range | Validation error | Must be 1-999999 |

## Error Handling

| Error | Condition | Resolution |
|-------|-----------|------------|
| Invalid ExecutionOrder | < 1 or > 999999 | Use value in range |
| Missing required property | Constructor not used, property not set | Set Message, EntityLogicalName, Stage |
| Image on incompatible stage | PostImage on PreOperation | Use correct stage for image type |

## Dependencies

- **Internal:** None (pure attribute definitions)
- **External:** None

## Configuration

### Attribute Usage

```csharp
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
```

Both attributes:
- Target classes only (not methods or properties)
- Allow multiple instances per class
- Do not inherit to derived classes

### Execution Order

```csharp
public const int MinExecutionOrder = 1;
public const int MaxExecutionOrder = 999999;
```

Lower values execute first within the same stage.

## Thread Safety

Attributes are immutable after construction. Thread safety is inherent to the attribute pattern - instances are created once at assembly load time.

## Related

- [PPDS.Plugins: Analyzers](02-analyzers.md) - Compile-time validation
- `src/PPDS.Cli/Plugins/Extraction/AssemblyExtractor.cs` - Attribute extraction

## Source Files

| File | Purpose |
|------|---------|
| `src/PPDS.Plugins/Attributes/PluginStepAttribute.cs` | Step registration attribute |
| `src/PPDS.Plugins/Attributes/PluginImageAttribute.cs` | Image registration attribute |
| `src/PPDS.Plugins/Enums/PluginStage.cs` | Execution stage enum |
| `src/PPDS.Plugins/Enums/PluginMode.cs` | Execution mode enum |
| `src/PPDS.Plugins/Enums/PluginImageType.cs` | Image type enum |
| `src/PPDS.Cli/Plugins/Extraction/AssemblyExtractor.cs` | Attribute extraction |
| `tests/PPDS.Plugins.Tests/PluginStepAttributeTests.cs` | Step attribute tests |
| `tests/PPDS.Plugins.Tests/PluginImageAttributeTests.cs` | Image attribute tests |
| `tests/PPDS.Plugins.Tests/EnumTests.cs` | Enum value tests |
