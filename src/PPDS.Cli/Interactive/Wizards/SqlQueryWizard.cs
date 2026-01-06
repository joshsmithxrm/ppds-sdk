using Microsoft.Extensions.DependencyInjection;
using PPDS.Auth.Profiles;
using PPDS.Cli.Infrastructure;
using PPDS.Cli.Interactive.Components;
using PPDS.Dataverse.Query;
using PPDS.Dataverse.Sql.Parsing;
using PPDS.Dataverse.Sql.Transpilation;
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
    public static async Task RunAsync(AuthProfile profile, CancellationToken cancellationToken)
    {
        if (profile.Environment == null)
        {
            AnsiConsole.MarkupLine(Styles.ErrorText("No environment selected."));
            AnsiConsole.MarkupLine(Styles.MutedText("Select an environment first from the main menu."));
            WaitForKey();
            return;
        }

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

            // Get SQL query from user
            var sql = AnsiConsole.Prompt(
                new TextPrompt<string>("[grey]Enter SQL query (or 'back' to return):[/]")
                    .AllowEmpty());

            if (string.IsNullOrWhiteSpace(sql) || sql.Equals("back", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            // Execute the query
            await ExecuteQueryAsync(profile, sql, cancellationToken);
        }
    }

    private static async Task ExecuteQueryAsync(
        AuthProfile profile,
        string sql,
        CancellationToken cancellationToken)
    {
        try
        {
            // Parse and transpile SQL
            AnsiConsole.WriteLine();
            var fetchXml = await AnsiConsole.Status()
                .Spinner(Spinner.Known.Dots)
                .SpinnerStyle(Styles.Primary)
                .StartAsync("Parsing SQL...", _ =>
                {
                    var parser = new SqlParser(sql);
                    var ast = parser.Parse();
                    var transpiler = new SqlToFetchXmlTranspiler();
                    return Task.FromResult(transpiler.Transpile(ast));
                });

            // Execute query
            var result = await AnsiConsole.Status()
                .Spinner(Spinner.Known.Dots)
                .SpinnerStyle(Styles.Primary)
                .StartAsync("Executing query...", async ctx =>
                {
                    await using var serviceProvider = await ProfileServiceFactory.CreateFromProfileAsync(
                        profile.Name,
                        profile.Environment!.Url,
                        deviceCodeCallback: ProfileServiceFactory.DefaultDeviceCodeCallback,
                        cancellationToken: cancellationToken);

                    var queryExecutor = serviceProvider.GetRequiredService<IQueryExecutor>();
                    return await queryExecutor.ExecuteFetchXmlAsync(
                        fetchXml,
                        pageNumber: null,
                        pagingCookie: null,
                        includeCount: false,
                        cancellationToken);
                });

            // Display results
            DisplayResults(result);

            // Handle paging
            if (result.MoreRecords && !string.IsNullOrEmpty(result.PagingCookie))
            {
                await HandlePagingAsync(profile, fetchXml, result, cancellationToken);
            }
            else
            {
                WaitForKey();
            }
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
        }
        catch (Exception ex)
        {
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine(Styles.ErrorText($"Error: {ex.Message}"));
            WaitForKey();
        }
    }

    private static void DisplayResults(QueryResult result)
    {
        AnsiConsole.WriteLine();

        if (result.Count == 0)
        {
            AnsiConsole.MarkupLine(Styles.WarningText("No records found."));
            return;
        }

        // Create table
        var table = new Table()
            .Border(TableBorder.Rounded)
            .BorderColor(Styles.Primary);

        // Add columns
        foreach (var column in result.Columns)
        {
            var header = column.Alias ?? column.LogicalName;
            table.AddColumn(new TableColumn(Markup.Escape(header)).Centered());
        }

        // Add rows (limit to 50 for display)
        var displayCount = Math.Min(result.Count, 50);
        foreach (var record in result.Records.Take(displayCount))
        {
            var cells = new List<string>();
            foreach (var column in result.Columns)
            {
                var key = column.Alias ?? column.LogicalName;
                if (record.TryGetValue(key, out var queryValue) && queryValue != null)
                {
                    var displayValue = queryValue.FormattedValue ?? queryValue.Value?.ToString() ?? "";
                    // Truncate long values
                    if (displayValue.Length > 40)
                    {
                        displayValue = displayValue[..37] + "...";
                    }
                    cells.Add(Markup.Escape(displayValue));
                }
                else
                {
                    cells.Add(Styles.MutedText("-"));
                }
            }
            table.AddRow(cells.ToArray());
        }

        AnsiConsole.Write(table);

        // Summary
        AnsiConsole.WriteLine();
        var summary = $"Entity: {Markup.Escape(result.EntityLogicalName)} | " +
                     $"Records: {result.Count}";

        if (result.TotalCount.HasValue && result.TotalCount != result.Count)
        {
            summary += $" of {result.TotalCount}";
        }

        summary += $" | Time: {result.ExecutionTimeMs}ms";

        if (result.Count > displayCount)
        {
            summary += $" | Showing first {displayCount}";
        }

        AnsiConsole.MarkupLine(Styles.MutedText(summary));

        if (result.MoreRecords)
        {
            AnsiConsole.MarkupLine(Styles.PrimaryText("More records available..."));
        }
    }

    private static async Task HandlePagingAsync(
        AuthProfile profile,
        string fetchXml,
        QueryResult currentResult,
        CancellationToken cancellationToken)
    {
        var page = 1;
        var pagingCookie = currentResult.PagingCookie;
        var hasMore = currentResult.MoreRecords;

        while (hasMore && !cancellationToken.IsCancellationRequested)
        {
            AnsiConsole.WriteLine();
            var action = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("[grey]What would you like to do?[/]")
                    .HighlightStyle(Styles.SelectionHighlight)
                    .AddChoices("Next page", "New query", "Back to menu"));

            if (action == "Back to menu")
            {
                return;
            }

            if (action == "New query")
            {
                break;
            }

            // Fetch next page
            page++;
            var result = await AnsiConsole.Status()
                .Spinner(Spinner.Known.Dots)
                .SpinnerStyle(Styles.Primary)
                .StartAsync($"Fetching page {page}...", async ctx =>
                {
                    await using var serviceProvider = await ProfileServiceFactory.CreateFromProfileAsync(
                        profile.Name,
                        profile.Environment!.Url,
                        deviceCodeCallback: ProfileServiceFactory.DefaultDeviceCodeCallback,
                        cancellationToken: cancellationToken);

                    var queryExecutor = serviceProvider.GetRequiredService<IQueryExecutor>();
                    return await queryExecutor.ExecuteFetchXmlAsync(
                        fetchXml,
                        pageNumber: page,
                        pagingCookie: pagingCookie,
                        includeCount: false,
                        cancellationToken);
                });

            AnsiConsole.Clear();
            DisplayResults(result);

            pagingCookie = result.PagingCookie;
            hasMore = result.MoreRecords;
        }

        if (!hasMore)
        {
            WaitForKey();
        }
    }

    private static void WaitForKey()
    {
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine(Styles.MutedText("Press any key to continue..."));
        Console.ReadKey(true);
    }
}
