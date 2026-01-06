using Microsoft.Extensions.DependencyInjection;
using PPDS.Auth.Profiles;
using PPDS.Cli.Infrastructure;
using PPDS.Cli.Interactive.Components;
using PPDS.Cli.Interactive.Components.QueryResults;
using PPDS.Cli.Services.Query;
using PPDS.Dataverse.Query;
using PPDS.Dataverse.Sql.Parsing;
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

            // Execute the query - returns true if user wants to enter another query
            var wantsNewQuery = await ExecuteQueryAsync(profile, sql, cancellationToken);
            if (!wantsNewQuery)
            {
                return; // User chose to go back
            }
        }
    }

    private static async Task<bool> ExecuteQueryAsync(
        AuthProfile profile,
        string sql,
        CancellationToken cancellationToken)
    {
        try
        {
            // Execute query via ISqlQueryService
            AnsiConsole.WriteLine();
            var queryResult = await AnsiConsole.Status()
                .Spinner(Spinner.Known.Dots)
                .SpinnerStyle(Styles.Primary)
                .StartAsync("Executing query...", async ctx =>
                {
                    await using var serviceProvider = await ProfileServiceFactory.CreateFromProfileAsync(
                        profile.Name,
                        profile.Environment!.Url,
                        deviceCodeCallback: ProfileServiceFactory.DefaultDeviceCodeCallback,
                        cancellationToken: cancellationToken);

                    var sqlQueryService = serviceProvider.GetRequiredService<ISqlQueryService>();
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
                    // Fetch additional pages on demand
                    await using var serviceProvider = await ProfileServiceFactory.CreateFromProfileAsync(
                        profile.Name,
                        profile.Environment!.Url,
                        deviceCodeCallback: ProfileServiceFactory.DefaultDeviceCodeCallback,
                        cancellationToken: cancellationToken);

                    var sqlQueryService = serviceProvider.GetRequiredService<ISqlQueryService>();
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
                cancellationToken);

            // Return whether the user wants a new query (true) or to go back (false)
            return showResult == QueryResultsViewer.ShowResult.NewQuery;
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
            return true; // Allow user to try another query
        }
        catch (Exception ex)
        {
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine(Styles.ErrorText($"Error: {ex.Message}"));
            WaitForKey();
            return true; // Allow user to try another query
        }
    }

    private static void WaitForKey()
    {
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine(Styles.MutedText("Press any key to continue..."));
        Console.ReadKey(true);
    }
}
