using PPDS.Auth.Profiles;
using PPDS.Cli.Interactive.Components;
using PPDS.Cli.Interactive.Components.QueryResults;
using PPDS.Cli.Services.Query;
using PPDS.Dataverse.Sql.Parsing;
using RadLine;
using Spectre.Console;

namespace PPDS.Cli.Interactive.Wizards;

/// <summary>
/// Interactive SQL query wizard.
/// </summary>
internal static class SqlQueryWizard
{
    /// <summary>
    /// Runs the SQL query wizard.
    /// </summary>
    /// <returns>WizardResult indicating whether to exit the CLI or continue.</returns>
    public static async Task<WizardResult> RunAsync(
        AuthProfile profile,
        InteractiveSession session,
        CancellationToken cancellationToken)
    {
        if (profile.Environment == null)
        {
            AnsiConsole.MarkupLine(Styles.ErrorText("No environment selected."));
            AnsiConsole.MarkupLine(Styles.MutedText("Select an environment first from the main menu."));
            WaitForKey();
            return WizardResult.Continue;
        }

        string? lastQuery = null;
        var environmentUrl = profile.Environment.Url;

        while (!cancellationToken.IsCancellationRequested)
        {
            AnsiConsole.Clear();

            // Show header
            var headerPanel = new Panel(
                $"{Styles.MutedText("Profile:")} {Markup.Escape(profile.DisplayIdentifier)}\n" +
                $"{Styles.MutedText("Environment:")} {Markup.Escape(profile.Environment.DisplayName)}")
            {
                Border = BoxBorder.Rounded,
                BorderStyle = Styles.HeaderBorder,
                Header = new PanelHeader(" SQL Query ", Justify.Center),
                Padding = new Padding(1, 0, 1, 0)
            };
            AnsiConsole.Write(headerPanel);
            AnsiConsole.WriteLine();

            // Get SQL query from user using RadLine for proper editing support
            // (arrow keys, home/end, ctrl+arrows for word nav, up/down for history)
            AnsiConsole.MarkupLine(Styles.MutedText("Enter SQL query (Up/Down for history, Esc to go back):"));

            // Build RadLine editor with history
            var editor = new LineEditor(AnsiConsole.Console)
            {
                Prompt = new LineEditorPrompt("> "),
                Text = lastQuery ?? string.Empty
            };

            // Track if Escape was pressed (RadLine default clears line, we want to go back)
            var escapePressed = false;
            editor.KeyBindings.Add(ConsoleKey.Escape, () =>
            {
                escapePressed = true;
                return new SubmitCommand();
            });

            // Sync QueryHistory to RadLine's history for up/down arrow navigation
            foreach (var historyItem in QueryHistory.Recent.Reverse())
            {
                editor.History.Add(historyItem);
            }

            var sql = await editor.ReadLine(cancellationToken) ?? string.Empty;

            // If Escape was pressed, go back regardless of input
            if (escapePressed)
            {
                return WizardResult.Continue;
            }

            // Handle special commands
            if (string.IsNullOrWhiteSpace(sql) || sql.Equals("back", StringComparison.OrdinalIgnoreCase))
            {
                return WizardResult.Continue;
            }

            if (sql.Equals("exit", StringComparison.OrdinalIgnoreCase))
            {
                return WizardResult.Exit;
            }

            // Add to history before execution (so failed queries are also recorded)
            QueryHistory.Add(sql);

            // Execute the query
            var result = await ExecuteQueryAsync(
                session,
                environmentUrl,
                profile.DisplayIdentifier,
                profile.Environment.DisplayName,
                sql,
                cancellationToken);

            switch (result.Outcome)
            {
                case QueryOutcome.NewQuery:
                    lastQuery = null; // Clear pre-fill for fresh query
                    break;

                case QueryOutcome.Back:
                    return WizardResult.Continue;

                case QueryOutcome.Exit:
                    return WizardResult.Exit;

                case QueryOutcome.Error:
                    lastQuery = sql; // Pre-fill with failed query for editing
                    break;
            }
        }

        return WizardResult.Continue;
    }

    private enum QueryOutcome
    {
        NewQuery,
        Back,
        Exit,
        Error
    }

    private readonly record struct ExecuteResult(QueryOutcome Outcome);

    private static async Task<ExecuteResult> ExecuteQueryAsync(
        InteractiveSession session,
        string environmentUrl,
        string profileName,
        string environmentName,
        string sql,
        CancellationToken cancellationToken)
    {
        try
        {
            // Execute query via ISqlQueryService (using session for connection reuse)
            AnsiConsole.WriteLine();
            var queryResult = await AnsiConsole.Status()
                .Spinner(Spinner.Known.Dots)
                .SpinnerStyle(Styles.Primary)
                .StartAsync("Executing query...", async ctx =>
                {
                    var sqlQueryService = await session.GetSqlQueryServiceAsync(
                        environmentUrl, cancellationToken);

                    var request = new SqlQueryRequest
                    {
                        Sql = sql,
                        IncludeCount = false
                    };

                    return await sqlQueryService.ExecuteAsync(request, cancellationToken);
                });

            // Display results with the new viewer
            var showResult = await QueryResultsViewer.ShowAsync(
                queryResult.Result,
                async (pageNumber, pagingCookie) =>
                {
                    // Fetch additional pages on demand (reuses session pool)
                    var sqlQueryService = await session.GetSqlQueryServiceAsync(
                        environmentUrl, cancellationToken);

                    var request = new SqlQueryRequest
                    {
                        Sql = sql,
                        PageNumber = pageNumber,
                        PagingCookie = pagingCookie,
                        IncludeCount = false
                    };

                    var result = await sqlQueryService.ExecuteAsync(request, cancellationToken);
                    return result.Result;
                },
                environmentUrl,
                profileName,
                environmentName,
                cancellationToken);

            // Map view result to query outcome
            return showResult switch
            {
                QueryResultsViewer.ShowResult.NewQuery => new ExecuteResult(QueryOutcome.NewQuery),
                QueryResultsViewer.ShowResult.Exit => new ExecuteResult(QueryOutcome.Exit),
                _ => new ExecuteResult(QueryOutcome.Back)
            };
        }
        catch (SqlParseException ex)
        {
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine(Styles.ErrorText("SQL Parse Error:"));
            AnsiConsole.MarkupLine(Markup.Escape(ex.Message));
            if (!string.IsNullOrEmpty(ex.ContextSnippet))
            {
                AnsiConsole.WriteLine();
                AnsiConsole.MarkupLine(Styles.MutedText($"Near: {ex.ContextSnippet}"));
            }
            WaitForKey();
            return new ExecuteResult(QueryOutcome.Error);
        }
        catch (Exception ex)
        {
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine(Styles.ErrorText($"Error: {ex.Message}"));
            WaitForKey();
            return new ExecuteResult(QueryOutcome.Error);
        }
    }

    private static void WaitForKey()
    {
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine(Styles.MutedText("Press any key to continue..."));
        Console.ReadKey(true);
    }
}
