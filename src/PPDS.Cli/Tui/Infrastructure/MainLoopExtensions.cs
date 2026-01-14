using Terminal.Gui;

namespace PPDS.Cli.Tui.Infrastructure;

/// <summary>
/// Extension methods for Terminal.Gui MainLoop with error handling.
/// </summary>
internal static class MainLoopExtensions
{
    /// <summary>
    /// Invokes an action on the main loop with exception handling.
    /// Exceptions are caught and reported to the error service instead of crashing the TUI.
    /// </summary>
    /// <param name="mainLoop">The main loop to invoke on.</param>
    /// <param name="action">The action to execute.</param>
    /// <param name="errorService">The error service to report exceptions to.</param>
    /// <param name="context">Context string for error reporting (e.g., "LoadQueryResults").</param>
    public static void SafeInvoke(
        this MainLoop? mainLoop,
        Action action,
        ITuiErrorService errorService,
        string context)
    {
        if (mainLoop == null)
        {
            TuiDebugLog.Log($"SafeInvoke: MainLoop is null, cannot invoke {context}");
            return;
        }

        mainLoop.Invoke(() =>
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                errorService.ReportError($"Error in {context}", ex, context);
                TuiDebugLog.Log($"SafeInvoke caught exception in {context}: {ex}");
            }
        });
    }

    /// <summary>
    /// Invokes an action on the main loop with exception handling and a custom error message.
    /// Exceptions are caught and reported to the error service instead of crashing the TUI.
    /// </summary>
    /// <param name="mainLoop">The main loop to invoke on.</param>
    /// <param name="action">The action to execute.</param>
    /// <param name="errorService">The error service to report exceptions to.</param>
    /// <param name="context">Context string for error reporting.</param>
    /// <param name="errorMessage">User-friendly error message.</param>
    public static void SafeInvoke(
        this MainLoop? mainLoop,
        Action action,
        ITuiErrorService errorService,
        string context,
        string errorMessage)
    {
        if (mainLoop == null)
        {
            TuiDebugLog.Log($"SafeInvoke: MainLoop is null, cannot invoke {context}");
            return;
        }

        mainLoop.Invoke(() =>
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                errorService.ReportError(errorMessage, ex, context);
                TuiDebugLog.Log($"SafeInvoke caught exception in {context}: {ex}");
            }
        });
    }
}
