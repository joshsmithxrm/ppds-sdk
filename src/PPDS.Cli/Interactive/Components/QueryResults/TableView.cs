namespace PPDS.Cli.Interactive.Components.QueryResults;

/// <summary>
/// Result of a view operation.
/// </summary>
internal enum ViewResult
{
    /// <summary>User wants to go back to the main menu.</summary>
    Back,
    /// <summary>User wants to enter a new query.</summary>
    NewQuery,
    /// <summary>User wants to switch to record view.</summary>
    SwitchToRecordView,
    /// <summary>User wants to switch to table view.</summary>
    SwitchToTableView,
    /// <summary>User wants to exit the entire interactive CLI.</summary>
    Exit
}
