# TUI Troubleshooting

## Debug Log

When troubleshooting TUI issues (`ppds -i`), **always check the debug log first**:

```
~/.ppds/tui-debug.log
```

**Windows:** `C:\Users\<username>\.ppds\tui-debug.log`

The log contains:
- Timestamps with millisecond precision
- Thread IDs
- Caller file/line/method
- Status updates and error messages

### Example log output:
```
[14:32:01.234] T001 SqlQueryScreen.cs:216 ExecuteQueryAsync: Starting query execution for: https://org.crm.dynamics.com
[14:32:01.235] T001 SqlQueryScreen.cs:222 ExecuteQueryAsync: Getting SQL query service...
[14:32:01.456] T001 SqlQueryScreen.cs:225 ExecuteQueryAsync: Got service, executing query...
[14:32:02.789] T001 SqlQueryScreen.cs:238 ExecuteQueryAsync: Query complete: 100 rows in 1333ms
```

### When to check:
- Query execution hangs or fails
- Auth prompts not appearing
- Status not updating
- Any TUI behavior seems wrong

### Log is cleared on each TUI session start
Each `ppds -i` run clears the previous log, so you always see the current session.

---

## Common TUI Issues

| Symptom | Check | Likely Cause |
|---------|-------|--------------|
| Query hangs at "Executing..." | Debug log for errors | Dataverse API error, deadlock |
| Auth messages in text area | Should be fixed | `AuthenticationOutput.Writer` not suppressed |
| UI doesn't update | Debug log for status | `MainLoop.Invoke()` not called |
| Dialog doesn't close | Debug log | Nested `Application.Run()` (deadlock) |

---

## Terminal.Gui Patterns

### Safe from MainLoop.Invoke:
- `MessageBox.Query()` / `MessageBox.ErrorQuery()` - modal, blocks, returns
- Direct property updates (`_label.Text = "..."`)
- `Application.Refresh()`

### UNSAFE (causes deadlock):
- `Application.Run(dialog)` inside `MainLoop.Invoke()` - **NEVER DO THIS**
- Nested event loops

### Async pattern:
```csharp
// Fire-and-forget with error handling
#pragma warning disable PPDS013
_ = DoWorkAsync().ContinueWith(t =>
{
    if (t.IsFaulted)
    {
        Application.MainLoop?.Invoke(() =>
            _statusLabel.Text = $"Error: {t.Exception?.InnerException?.Message}");
    }
}, TaskScheduler.Default);
#pragma warning restore PPDS013
```
