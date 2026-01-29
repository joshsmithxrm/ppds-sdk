# TUI Environment Dashboard

**Status:** Draft
**Version:** 1.0
**Last Updated:** 2026-01-28
**Code:** None

---

## Overview

The Environment Dashboard screen provides an at-a-glance view of environment administration state, combining five small Dataverse services into a single tabbed interface. Users switch between sub-views for Users & Roles, Cloud Flows, Environment Variables, and Connection References using hotkeys or a top selector bar. Each sub-view uses a DataTableView with service-specific columns and actions.

### Goals

- **Unified Admin View**: Single screen for environment-level administration tasks
- **User & Role Management**: Browse users, view role assignments, assign/remove roles
- **Flow Control**: Enable/disable cloud flows, view flow state and connection usage
- **Environment Variables**: View definitions and current values, edit values in-place
- **Connection Health**: Inspect connection references, identify orphans and unbound references

### Non-Goals

- Solution-level component management (handled by [tui-solutions.md](./tui-solutions.md))
- Security role creation or deletion (use Power Platform admin center)
- Flow editing or design (use Power Automate designer)
- Metadata/schema browsing (future screen)
- Deployment settings generation (use CLI `ppds deploy settings`)

---

## Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                        TuiShell                                  │
│       Tools > Environment Dashboard (new menu item)              │
└──────────────────────────────┬──────────────────────────────────┘
                               │
                               ▼
┌─────────────────────────────────────────────────────────────────┐
│                EnvironmentDashboardScreen                         │
│           (ITuiScreen + ITuiStateCapture)                        │
│                                                                  │
│  ┌──────────────────────────────────────────────────────────┐   │
│  │ [1] Users   [2] Flows   [3] Env Vars   [4] Conn Refs    │   │
│  ├──────────────────────────────────────────────────────────┤   │
│  │                                                           │   │
│  │ DataTableView (content varies by active sub-view)         │   │
│  │                                                           │   │
│  ├──────────────────────────────────────────────────────────┤   │
│  │ Detail Panel: properties of selected row                  │   │
│  ├──────────────────────────────────────────────────────────┤   │
│  │ Status: 142 users | Filter: "josh" | Sub-view: Users      │   │
│  └──────────────────────────────────────────────────────────┘   │
└──────────────┬─────────┬──────────┬─────────┬───────────────────┘
               │         │          │         │
               ▼         ▼          ▼         ▼
         ┌──────────┐ ┌────────┐ ┌────────┐ ┌──────────┐
         │IUserSvc  │ │IFlowSvc│ │IEnvVar │ │IConnRef  │
         │IRoleSvc  │ │        │ │  Svc   │ │  Svc     │
         └──────────┘ └────────┘ └────────┘ └──────────┘
               │         │          │         │
               └─────────┴──────────┴─────────┘
                          │
                          ▼
         ┌─────────────────────────────────────┐
         │      IDataverseConnectionPool       │
         └─────────────────────────────────────┘
```

### Components

| Component | Responsibility |
|-----------|----------------|
| `EnvironmentDashboardScreen` | Main screen: sub-view selector, shared DataTableView, detail panel |
| Users sub-view | User list with role display and assignment/removal |
| Flows sub-view | Cloud flow list with enable/disable toggle |
| EnvVars sub-view | Environment variable definitions with editable values |
| ConnRefs sub-view | Connection references with orphan analysis |

### Dependencies

- Depends on: [dataverse-services.md](./dataverse-services.md) for `IUserService`, `IRoleService`, `IFlowService`, `IEnvironmentVariableService`, `IConnectionReferenceService`
- Depends on: [tui.md](./tui.md) for `ITuiScreen`, `TuiDialog`, `IHotkeyRegistry`, `ITuiErrorService`
- Uses patterns from: [architecture.md](./architecture.md) for Application Service boundary

---

## Specification

### Core Requirements

1. Screen implements `ITuiScreen` and `ITuiStateCapture<EnvironmentDashboardScreenState>`
2. Four sub-views selectable via number keys (1-4) or RadioGroup selector bar
3. Each sub-view uses a DataTableView with service-specific columns
4. Sub-view data loads on first activation; cached until manual refresh (F5)
5. Detail panel below the table shows properties of the selected row
6. Each sub-view has its own filter bar for text search
7. All service calls run on background thread; UI updates via `Application.MainLoop.Invoke()`

### Primary Flows

**Sub-View: Users & Roles (1):**

1. **Load**: `IUserService.ListAsync()` populates table with columns: FullName, DomainName, Email, IsDisabled
2. **Search**: Filter bar searches across FullName, DomainName, Email (OR logic via filter parameter)
3. **View roles**: Select user → detail panel shows assigned roles via `IUserService.GetUserRolesAsync(userId)`
4. **Assign role**: Ctrl+A opens role selection dialog → `IRoleService.AssignRoleAsync(userId, roleId)`
5. **Remove role**: Select role in detail → Delete → `IRoleService.RemoveRoleAsync(userId, roleId)` with confirmation
6. **Toggle disabled**: Checkbox or hotkey to include/exclude disabled users

**Sub-View: Cloud Flows (2):**

1. **Load**: `IFlowService.ListAsync()` populates table with columns: DisplayName, State (On/Off), Category, Solution
2. **Filter**: Filter bar searches by display name; solution dropdown for solution filtering
3. **Toggle state**: Space on a flow row toggles On/Off (calls enable/disable API)
4. **View details**: Select flow → detail panel shows connection references used, owner, created/modified dates
5. **State filter**: Hotkey to show only On, only Off, or All flows

**Sub-View: Environment Variables (3):**

1. **Load**: `IEnvironmentVariableService.ListAsync()` populates table with columns: DisplayName, SchemaName, Type, CurrentValue, DefaultValue
2. **Filter**: Filter bar searches by display name or schema name
3. **Edit value**: Enter on row opens inline edit or dialog for `IEnvironmentVariableService.SetValueAsync(schemaName, value)`
4. **View details**: Detail panel shows full definition including description, type constraints
5. **Secret vars**: Secret-type variables show "(secret)" instead of value; editing requires confirmation

**Sub-View: Connection References (4):**

1. **Load**: `IConnectionReferenceService.ListAsync()` populates table with columns: DisplayName, LogicalName, ConnectorId, IsBound, Solution
2. **Filter**: Filter bar searches by display name or logical name
3. **Unbound only**: Toggle to show only unbound connection references
4. **Analyze**: Ctrl+Shift+A runs `AnalyzeAsync()` to identify orphans and missing references
5. **View flows**: Select CR → detail panel shows flows using it via `GetFlowsUsingAsync(logicalName)`
6. **Analysis results**: Highlight orphaned CRs (not used by any flow) and missing CRs (referenced by flows but not present)

### Constraints

- Sub-view data is independent; switching sub-views does not reload unless F5 is pressed
- Role assignment changes take effect immediately in Dataverse
- Environment variable edits take effect immediately; no undo
- Flow enable/disable may take a few seconds to propagate in Dataverse
- Connection reference analysis may be slow for large solutions (fetches all flows)

### Validation Rules

| Field | Rule | Error |
|-------|------|-------|
| Env var value | Must match type (string, number, boolean, JSON) | "Value must be a valid {type}" |
| Role assignment | User and role must exist | "User or role not found" |
| Flow toggle | Flow must be in a toggleable state | "Flow cannot be toggled in current state" |

---

## Core Types

### EnvironmentDashboardScreen

```csharp
internal sealed class EnvironmentDashboardScreen
    : ITuiScreen, ITuiStateCapture<EnvironmentDashboardScreenState>
{
    public EnvironmentDashboardScreen(
        InteractiveSession session,
        ITuiErrorService errorService,
        Action<DeviceCodeInfo>? deviceCodeCallback = null);

    // ITuiScreen
    public View Content { get; }
    public string Title { get; }  // "Environment Dashboard - {environment}"
    public MenuBarItem[]? ScreenMenuItems { get; }
    public Action? ExportAction { get; }

    // ITuiStateCapture
    public EnvironmentDashboardScreenState CaptureState();
}
```

### EnvironmentDashboardScreenState

```csharp
public sealed record EnvironmentDashboardScreenState(
    DashboardSubView ActiveSubView,
    int UserCount,
    int FlowCount,
    int EnvVarCount,
    int ConnRefCount,
    Guid? SelectedItemId,
    string? SelectedItemName,
    bool IsLoading,
    string? FilterText,
    bool ShowDisabledUsers,
    string? FlowStateFilter,    // "On", "Off", or null (All)
    bool UnboundOnly,
    string? ErrorMessage);
```

### DashboardSubView

```csharp
public enum DashboardSubView
{
    Users = 1,
    Flows = 2,
    EnvironmentVariables = 3,
    ConnectionReferences = 4
}
```

### RoleAssignmentDialog

Dialog for selecting a role to assign to a user.

```csharp
internal sealed class RoleAssignmentDialog
    : TuiDialog, ITuiStateCapture<RoleAssignmentDialogState>
{
    public RoleAssignmentDialog(
        List<RoleInfo> availableRoles,
        string userName,
        InteractiveSession? session = null);

    public RoleInfo? SelectedRole { get; }
    public RoleAssignmentDialogState CaptureState();
}
```

### RoleAssignmentDialogState

```csharp
public sealed record RoleAssignmentDialogState(
    string UserName,
    int AvailableRoleCount,
    string? SelectedRoleName,
    string? FilterText);
```

### EnvVarEditDialog

Dialog for editing an environment variable value.

```csharp
internal sealed class EnvVarEditDialog
    : TuiDialog, ITuiStateCapture<EnvVarEditDialogState>
{
    public EnvVarEditDialog(
        EnvironmentVariableInfo envVar,
        InteractiveSession? session = null);

    public string? NewValue { get; }
    public bool IsConfirmed { get; }
    public EnvVarEditDialogState CaptureState();
}
```

### EnvVarEditDialogState

```csharp
public sealed record EnvVarEditDialogState(
    string SchemaName,
    string DisplayName,
    string Type,
    string? CurrentValue,
    string? NewValue,
    bool IsSecret,
    bool IsConfirmed);
```

### Usage Pattern

```csharp
// TuiShell creates and navigates to the screen
var screen = new EnvironmentDashboardScreen(_session, _errorService, _deviceCodeCallback);
NavigateTo(screen);

// Screen loads sub-view data:
var provider = await _session.GetServiceProviderAsync(_environmentUrl);

// Users sub-view:
var userService = provider.GetRequiredService<IUserService>();
var users = await userService.ListAsync(filter: searchText);
var roles = await userService.GetUserRolesAsync(selectedUserId);

// Flows sub-view:
var flowService = provider.GetRequiredService<IFlowService>();
var flows = await flowService.ListAsync(state: flowStateFilter);

// Env vars sub-view:
var envVarService = provider.GetRequiredService<IEnvironmentVariableService>();
var envVars = await envVarService.ListAsync();
await envVarService.SetValueAsync(schemaName, newValue);

// Conn refs sub-view:
var connRefService = provider.GetRequiredService<IConnectionReferenceService>();
var connRefs = await connRefService.ListAsync(unboundOnly: unboundFilter);
var analysis = await connRefService.AnalyzeAsync();
```

---

## Error Handling

### Error Types

| Error | Condition | Recovery |
|-------|-----------|----------|
| Service unavailable | No connection to environment | Status line error; F5 to retry |
| Authentication expired | Token expired mid-operation | Re-authentication dialog |
| Insufficient privileges | Role assignment without admin rights | Error dialog with required role |
| Env var type mismatch | Value doesn't match variable type | Inline validation in edit dialog |
| Flow toggle failed | Flow in transitional state | Retry after delay; show current state |

### Recovery Strategies

- **Connection errors**: Status line error, F5 retries current sub-view
- **Auth errors**: `ReAuthenticationDialog` → retry pending operation
- **Privilege errors**: Error dialog explaining required security role
- **Validation errors**: Inline field feedback in edit dialogs
- **Analysis timeout**: Connection reference analysis may time out on very large environments; show partial results

### Edge Cases

| Scenario | Expected Behavior |
|----------|-------------------|
| No users (impossible but defensive) | Empty table with "No users found" |
| User with 0 roles | Detail panel shows "No roles assigned" with assign button |
| Flow in transitioning state | Show state as "Turning On..." or "Turning Off..." |
| Secret env var | Show "(secret)" for value; edit requires confirmation |
| Orphaned connection reference | Highlighted row with warning icon |
| Very large user list (10,000+) | Server-side pagination via `top` parameter |

---

## Design Decisions

### Why Tabbed Sub-Views Instead of Separate Screens?

**Context:** Five services (Users, Roles, Flows, EnvVars, ConnRefs) could each be their own screen. However, each would be a shallow table-with-detail screen used infrequently.

**Decision:** Combine into a single dashboard with hotkey-switchable sub-views (1-4).

**Alternatives considered:**
- Five separate screens: Rejected — clutters navigation menu with rarely-used screens
- Single flat screen showing all at once: Rejected — too much data, no focus
- Collapsible panels: Rejected — Terminal.Gui doesn't support smooth collapse/expand

**Consequences:**
- Positive: Single menu entry for all environment admin tasks
- Positive: Quick switching between related admin views (1→2→3→4)
- Positive: Shared DataTableView and detail panel infrastructure across sub-views
- Negative: Sub-view switching is custom (no native tab control in Terminal.Gui 1.19)
- Mitigation: RadioGroup at top provides visual tab indicator

### Why RadioGroup for Sub-View Selection?

**Context:** Terminal.Gui 1.19 lacks a TabView control. Need a sub-view selector.

**Decision:** Horizontal `RadioGroup` at the top of the content area, plus number key hotkeys (1-4) for keyboard-driven switching.

**Alternatives considered:**
- MenuBarItem sub-menu: Rejected — too hidden; users won't discover sub-views
- Buttons row: Possible but RadioGroup has built-in selection state
- Custom tab-like view: Over-engineering for 4 options

**Consequences:**
- Positive: Clear visual indicator of active sub-view
- Positive: Both mouse (click radio) and keyboard (1-4 hotkeys) selection
- Negative: Takes one row of vertical space from content area

### Why Separate Role Assignment Dialog?

**Context:** Role assignment could be inline (select role from dropdown in detail panel) or via modal dialog.

**Decision:** Modal dialog with searchable role list. Roles can number in the hundreds; a dropdown would be unwieldy.

**Alternatives considered:**
- Inline dropdown: Rejected — too many roles to browse in a dropdown
- Autocomplete text field: Possible but less discoverable for users unfamiliar with role names

**Consequences:**
- Positive: Full role list with search/filter
- Positive: Confirmation before assignment
- Negative: Extra dialog step for a simple action

---

## Configuration

| Setting | Type | Default | Description |
|---------|------|---------|-------------|
| DefaultSubView | DashboardSubView | Users | Sub-view shown on screen open |
| ShowDisabledUsers | bool | false | Include disabled users in user list |
| UserListTop | int | 250 | Max users per query |

---

## Testing

### Acceptance Criteria

- [ ] Screen loads with Users sub-view active by default
- [ ] Number keys 1-4 switch between sub-views
- [ ] RadioGroup reflects active sub-view
- [ ] Users sub-view loads users from IUserService.ListAsync
- [ ] Selecting a user shows their roles in the detail panel
- [ ] Ctrl+A opens role assignment dialog
- [ ] Flows sub-view loads flows from IFlowService.ListAsync
- [ ] Space on flow row toggles enabled state
- [ ] Env vars sub-view loads variables from IEnvironmentVariableService.ListAsync
- [ ] Enter on env var opens edit dialog
- [ ] Conn refs sub-view loads references from IConnectionReferenceService.ListAsync
- [ ] Unbound-only toggle filters to unbound references
- [ ] Ctrl+Shift+A runs connection reference analysis
- [ ] F5 refreshes current sub-view
- [ ] State capture returns accurate EnvironmentDashboardScreenState

### Edge Cases

| Scenario | Input | Expected Output |
|----------|-------|-----------------|
| Switch sub-view while loading | Press 2 during Users load | Cancel Users load, start Flows load |
| Assign role already assigned | Role already on user | "Role already assigned" message |
| Edit secret env var | Enter on secret variable | Confirmation dialog warns about secret type |
| Analyze empty environment | No flows or CRs | "No connection references to analyze" |
| User with many roles (50+) | Detail panel role list | Scrollable role list in detail panel |

### Test Examples

```csharp
[Fact]
[Trait("Category", "TuiUnit")]
public void EnvironmentDashboardScreen_DefaultsToUsersSubView()
{
    var session = CreateMockSession();
    var screen = new EnvironmentDashboardScreen(session, new TuiErrorService());

    var state = screen.CaptureState();

    Assert.Equal(DashboardSubView.Users, state.ActiveSubView);
}

[Fact]
[Trait("Category", "TuiUnit")]
public void EnvironmentDashboardScreen_SwitchesSubView()
{
    var session = CreateMockSession();
    var screen = new EnvironmentDashboardScreen(session, new TuiErrorService());
    screen.SwitchToSubView(DashboardSubView.Flows);

    var state = screen.CaptureState();

    Assert.Equal(DashboardSubView.Flows, state.ActiveSubView);
}

[Fact]
[Trait("Category", "TuiUnit")]
public void RoleAssignmentDialog_CapturesAvailableRoles()
{
    var roles = new List<RoleInfo>
    {
        new() { Name = "System Administrator" },
        new() { Name = "Basic User" }
    };
    var dialog = new RoleAssignmentDialog(roles, "Josh Smith");

    var state = dialog.CaptureState();

    Assert.Equal("Josh Smith", state.UserName);
    Assert.Equal(2, state.AvailableRoleCount);
    Assert.Null(state.SelectedRoleName);
}

[Fact]
[Trait("Category", "TuiUnit")]
public void EnvVarEditDialog_CapturesSecretFlag()
{
    var envVar = new EnvironmentVariableInfo
    {
        SchemaName = "ppds_ApiKey",
        DisplayName = "API Key",
        Type = "Secret",
        CurrentValue = null
    };
    var dialog = new EnvVarEditDialog(envVar);

    var state = dialog.CaptureState();

    Assert.True(state.IsSecret);
    Assert.Equal("ppds_ApiKey", state.SchemaName);
}
```

---

## Related Specs

- [dataverse-services.md](./dataverse-services.md) - Backend: IUserService, IRoleService, IFlowService, IEnvironmentVariableService, IConnectionReferenceService
- [tui.md](./tui.md) - TUI framework: ITuiScreen, TuiDialog, DataTableView, state capture
- [tui-solutions.md](./tui-solutions.md) - Navigate from solution components to environment dashboard sub-views
- [architecture.md](./architecture.md) - Application Service boundary pattern

---

## Roadmap

- Metadata browser sub-view (entity/attribute/relationship browsing)
- Bulk role assignment (assign role to multiple users)
- Flow trigger from TUI (trigger on-demand flows)
- Connection reference binding (update connection ID from TUI)
- Export sub-view data to CSV
