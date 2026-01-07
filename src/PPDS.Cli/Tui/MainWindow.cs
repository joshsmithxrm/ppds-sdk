using Microsoft.Extensions.Logging.Abstractions;
using PPDS.Auth.Credentials;
using PPDS.Auth.Profiles;
using PPDS.Cli.Services.Environment;
using PPDS.Cli.Services.Profile;
using PPDS.Cli.Tui.Dialogs;
using PPDS.Cli.Tui.Screens;
using Terminal.Gui;

namespace PPDS.Cli.Tui;

/// <summary>
/// Main window containing the menu and navigation for the TUI application.
/// </summary>
internal sealed class MainWindow : Window
{
    private string? _profileName;
    private string? _environmentName;
    private string? _environmentUrl;
    private readonly Action<DeviceCodeInfo>? _deviceCodeCallback;
    private readonly InteractiveSession _session;
    private readonly Label _statusLabel;

    public MainWindow(string? profileName, Action<DeviceCodeInfo>? deviceCodeCallback, InteractiveSession session)
    {
        _profileName = profileName;
        _deviceCodeCallback = deviceCodeCallback;
        _session = session;

        Title = "PPDS - Power Platform Developer Suite";
        X = 0;
        Y = 0;
        Width = Dim.Fill();
        Height = Dim.Fill();

        // Status bar at bottom
        _statusLabel = new Label
        {
            X = 0,
            Y = Pos.AnchorEnd(1),
            Width = Dim.Fill(),
            Height = 1,
            ColorScheme = new ColorScheme
            {
                Normal = Application.Driver.MakeAttribute(Color.White, Color.Blue)
            }
        };

        SetupMenu();
        ShowMainMenu();
        Add(_statusLabel);

        // Load initial profile info
        LoadProfileInfoAsync();
    }

    private void SetupMenu()
    {
        var menu = new MenuBar(new MenuBarItem[]
        {
            new("_File", new MenuItem[]
            {
                new("_SQL Query", "Run SQL queries against Dataverse", () => NavigateToSqlQuery(), shortcut: Key.F2),
                new("_Data Migration", "Import/export data", () => NavigateToDataMigration(), shortcut: Key.F3),
                new("", "", () => {}, null, null, Key.Null), // Separator
                new("Switch _Profile...", "Select a different authentication profile", () => ShowProfileSelector()),
                new("Switch _Environment...", "Select a different environment", () => ShowEnvironmentSelector()),
                new("", "", () => {}, null, null, Key.Null), // Separator
                new("_Quit", "Exit the application", () => RequestStop(), shortcut: Key.CtrlMask | Key.Q)
            }),
            new("_Tools", new MenuItem[]
            {
                new("_Solutions", "Browse solutions", () => ShowNotImplemented("Solutions browser")),
                new("_Plugin Traces", "View plugin traces", () => ShowNotImplemented("Plugin traces")),
            }),
            new("_Help", new MenuItem[]
            {
                new("_About", "About PPDS", () => ShowAbout()),
                new("_Keyboard Shortcuts", "Show keyboard shortcuts", () => ShowKeyboardShortcuts(), shortcut: Key.F1),
            })
        });

        Add(menu);
    }

    private void ShowMainMenu()
    {
        var container = new FrameView("Main Menu")
        {
            X = 0,
            Y = 1,
            Width = Dim.Fill(),
            Height = Dim.Fill() - 1 // Leave room for status bar
        };

        var label = new Label("Welcome to PPDS Interactive Mode\n\nSelect an option from the menu or use keyboard shortcuts:")
        {
            X = 2,
            Y = 1,
            Width = Dim.Fill() - 4,
            Height = 3
        };

        var buttonSql = new Button("SQL Query (F2)")
        {
            X = 2,
            Y = 5
        };
        buttonSql.Clicked += () => NavigateToSqlQuery();

        var buttonData = new Button("Data Migration (F3)")
        {
            X = 2,
            Y = 7
        };
        buttonData.Clicked += () => NavigateToDataMigration();

        var buttonQuit = new Button("Quit (Ctrl+Q)")
        {
            X = 2,
            Y = 10
        };
        buttonQuit.Clicked += () => RequestStop();

        container.Add(label, buttonSql, buttonData, buttonQuit);
        Add(container);

        // Global keyboard shortcuts
        KeyPress += (e) =>
        {
            switch (e.KeyEvent.Key)
            {
                case Key.F1:
                    ShowKeyboardShortcuts();
                    e.Handled = true;
                    break;
                case Key.F2:
                    NavigateToSqlQuery();
                    e.Handled = true;
                    break;
                case Key.F3:
                    NavigateToDataMigration();
                    e.Handled = true;
                    break;
            }
        };
    }

    private void LoadProfileInfoAsync()
    {
        // Fire-and-forget with error handling
#pragma warning disable PPDS013 // Fire-and-forget with explicit error handling
        _ = LoadProfileInfoInternalAsync().ContinueWith(t =>
        {
            if (t.IsFaulted && t.Exception != null)
            {
                Application.MainLoop?.Invoke(() =>
                {
                    UpdateStatus("Error loading profile info");
                });
            }
        }, TaskScheduler.Default);
#pragma warning restore PPDS013
    }

    private async Task LoadProfileInfoInternalAsync()
    {
        using var store = new ProfileStore();
        var collection = await store.LoadAsync(CancellationToken.None);
        var profile = collection.ActiveProfile;

        Application.MainLoop?.Invoke(() =>
        {
            if (profile != null)
            {
                _profileName = profile.DisplayIdentifier;
                _environmentName = profile.Environment?.DisplayName;
                _environmentUrl = profile.Environment?.Url;
            }
            UpdateStatus();
        });
    }

    private void UpdateStatus(string? error = null)
    {
        if (error != null)
        {
            _statusLabel.Text = $" {error}";
            return;
        }

        var profilePart = _profileName != null ? $"Profile: {_profileName}" : "No profile";
        var envPart = _environmentName != null ? $"Environment: {_environmentName}" : "No environment";
        _statusLabel.Text = $" {profilePart} | {envPart}";
    }

    private void ShowProfileSelector()
    {
        var store = new ProfileStore();
        var service = new ProfileService(store, NullLogger<ProfileService>.Instance);
        var dialog = new ProfileSelectorDialog(service);

        Application.Run(dialog);

        if (dialog.SelectedProfile != null)
        {
            // Update active profile and refresh
#pragma warning disable PPDS013 // Fire-and-forget with explicit error handling
            _ = SetActiveProfileAsync(dialog.SelectedProfile).ContinueWith(t =>
            {
                if (t.IsFaulted)
                {
                    Application.MainLoop?.Invoke(() =>
                    {
                        MessageBox.ErrorQuery("Error", t.Exception?.InnerException?.Message ?? "Failed to switch profile", "OK");
                    });
                }
            }, TaskScheduler.Default);
#pragma warning restore PPDS013
        }
        else if (dialog.CreateNewSelected)
        {
            // Profile creation would require more complex flow - show message for now
            MessageBox.Query("Create Profile", "Profile creation is available via CLI:\n\nppds auth create --name MyProfile", "OK");
        }
    }

    private async Task SetActiveProfileAsync(ProfileSummary profile)
    {
        var store = new ProfileStore();
        var service = new ProfileService(store, NullLogger<ProfileService>.Instance);

        await service.SetActiveProfileAsync(profile.DisplayIdentifier);

        // Reload profile info
        _profileName = profile.DisplayIdentifier;
        _environmentName = profile.EnvironmentName;
        _environmentUrl = profile.EnvironmentUrl;

        // Invalidate session to force reconnection with new profile
        await _session.InvalidateAsync();

        Application.MainLoop?.Invoke(() =>
        {
            UpdateStatus();
        });
    }

    private void ShowEnvironmentSelector()
    {
        if (_profileName == null)
        {
            MessageBox.ErrorQuery("No Profile", "Please select a profile first before choosing an environment.", "OK");
            return;
        }

        var store = new ProfileStore();
        var service = new EnvironmentService(store, NullLogger<EnvironmentService>.Instance);
        var dialog = new EnvironmentSelectorDialog(service, _deviceCodeCallback);

        Application.Run(dialog);

        if (dialog.SelectedEnvironment != null || dialog.UseManualUrl)
        {
            var url = dialog.UseManualUrl ? dialog.ManualUrl : dialog.SelectedEnvironment?.Url;
            var name = dialog.UseManualUrl ? dialog.ManualUrl : dialog.SelectedEnvironment?.DisplayName;

            if (url != null)
            {
#pragma warning disable PPDS013 // Fire-and-forget with explicit error handling
                _ = SetEnvironmentAsync(url, name).ContinueWith(t =>
                {
                    if (t.IsFaulted)
                    {
                        Application.MainLoop?.Invoke(() =>
                        {
                            MessageBox.ErrorQuery("Error", t.Exception?.InnerException?.Message ?? "Failed to set environment", "OK");
                        });
                    }
                }, TaskScheduler.Default);
#pragma warning restore PPDS013
            }
        }
    }

    private async Task SetEnvironmentAsync(string url, string? displayName)
    {
        var store = new ProfileStore();
        var service = new EnvironmentService(store, NullLogger<EnvironmentService>.Instance);

        var result = await service.SetEnvironmentAsync(url, _deviceCodeCallback);

        _environmentUrl = result.Url;
        _environmentName = result.DisplayName;

        // Invalidate session to force reconnection with new environment
        await _session.InvalidateAsync();

        Application.MainLoop?.Invoke(() =>
        {
            UpdateStatus();
        });
    }

    private void NavigateToSqlQuery()
    {
        var sqlScreen = new SqlQueryScreen(_profileName, _deviceCodeCallback, _session);
        Application.Run(sqlScreen);
    }

    private void NavigateToDataMigration()
    {
        ShowNotImplemented("Data Migration");
    }

    private void ShowNotImplemented(string feature)
    {
        MessageBox.Query("Not Implemented", $"{feature} is not yet implemented.\n\nThis feature is planned for a future release.", "OK");
    }

    private void ShowAbout()
    {
        var assembly = typeof(MainWindow).Assembly;
        var version = assembly.GetName().Version?.ToString() ?? "Unknown";
        MessageBox.Query("About PPDS",
            $"Power Platform Developer Suite\n\nVersion: {version}\n\nA multi-interface platform for Dataverse development.\n\nhttps://github.com/joshsmithxrm/ppds-sdk",
            "OK");
    }

    private void ShowKeyboardShortcuts()
    {
        MessageBox.Query("Keyboard Shortcuts",
            "Global Shortcuts:\n" +
            "  F2       - SQL Query\n" +
            "  F3       - Data Migration\n" +
            "  Ctrl+Q   - Quit\n\n" +
            "Table Navigation:\n" +
            "  Arrows   - Navigate cells\n" +
            "  PgUp/Dn  - Page up/down\n" +
            "  Home/End - First/last row\n" +
            "  /        - Filter results\n" +
            "  Ctrl+C   - Copy cell\n" +
            "  Ctrl+E   - Export\n",
            "OK");
    }
}
