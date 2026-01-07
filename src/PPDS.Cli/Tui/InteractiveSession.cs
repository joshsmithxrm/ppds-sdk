using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using PPDS.Auth.Credentials;
using PPDS.Auth.Profiles;
using PPDS.Cli.Infrastructure;
using PPDS.Cli.Services.Environment;
using PPDS.Cli.Services.Export;
using PPDS.Cli.Services.History;
using PPDS.Cli.Services.Profile;
using PPDS.Cli.Services.Query;
using PPDS.Cli.Tui.Infrastructure;
using PPDS.Dataverse.Pooling;

namespace PPDS.Cli.Tui;

/// <summary>
/// Manages the interactive session state including connection pool lifecycle.
/// The pool is lazily created on first use and reused across all queries in the session.
/// </summary>
/// <remarks>
/// This class ensures that:
/// - Connection pool is created once and reused for all queries (faster subsequent queries)
/// - DOP detection happens once per session
/// - Throttle state is preserved across queries
/// - Environment changes trigger pool recreation
/// </remarks>
internal sealed class InteractiveSession : IAsyncDisposable
{
    private readonly string _profileName;
    private readonly Action<DeviceCodeInfo>? _deviceCodeCallback;
    private readonly ProfileStore _profileStore;
    private readonly SemaphoreSlim _lock = new(1, 1);

    private ServiceProvider? _serviceProvider;
    private string? _currentEnvironmentUrl;
    private bool _disposed;

    /// <summary>
    /// Gets the current environment URL, or null if no connection has been established.
    /// </summary>
    public string? EnvironmentUrl => _currentEnvironmentUrl;

    /// <summary>
    /// Creates a new interactive session for the specified profile.
    /// </summary>
    /// <param name="profileName">The profile name (null for active profile).</param>
    /// <param name="profileStore">Shared profile store instance.</param>
    /// <param name="deviceCodeCallback">Callback for device code display.</param>
    public InteractiveSession(
        string? profileName,
        ProfileStore profileStore,
        Action<DeviceCodeInfo>? deviceCodeCallback = null)
    {
        _profileName = profileName ?? string.Empty;
        _profileStore = profileStore ?? throw new ArgumentNullException(nameof(profileStore));
        _deviceCodeCallback = deviceCodeCallback;
    }

    /// <summary>
    /// Gets or creates a service provider for the specified environment.
    /// If the environment changes, the existing provider is disposed and a new one is created.
    /// </summary>
    /// <param name="environmentUrl">The environment URL to connect to.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The service provider with connection pool.</returns>
    public async Task<ServiceProvider> GetServiceProviderAsync(
        string environmentUrl,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        await _lock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            // Check if we need to create or recreate the provider
            if (_serviceProvider != null && _currentEnvironmentUrl == environmentUrl)
            {
                return _serviceProvider;
            }

            // Dispose existing provider if environment changed
            if (_serviceProvider != null)
            {
                await _serviceProvider.DisposeAsync().ConfigureAwait(false);
                _serviceProvider = null;
                _currentEnvironmentUrl = null;
            }

            // Create new provider
            _serviceProvider = await ProfileServiceFactory.CreateFromProfileAsync(
                string.IsNullOrEmpty(_profileName) ? null : _profileName,
                environmentUrl,
                deviceCodeCallback: _deviceCodeCallback,
                cancellationToken: cancellationToken).ConfigureAwait(false);

            _currentEnvironmentUrl = environmentUrl;
            return _serviceProvider;
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <summary>
    /// Gets the SQL query service for the specified environment.
    /// The underlying connection pool is reused across calls.
    /// </summary>
    /// <param name="environmentUrl">The environment URL to connect to.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The SQL query service.</returns>
    public async Task<ISqlQueryService> GetSqlQueryServiceAsync(
        string environmentUrl,
        CancellationToken cancellationToken = default)
    {
        var provider = await GetServiceProviderAsync(environmentUrl, cancellationToken).ConfigureAwait(false);
        return provider.GetRequiredService<ISqlQueryService>();
    }

    /// <summary>
    /// Gets the connection pool for the specified environment.
    /// </summary>
    /// <param name="environmentUrl">The environment URL to connect to.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The connection pool.</returns>
    public async Task<IDataverseConnectionPool> GetConnectionPoolAsync(
        string environmentUrl,
        CancellationToken cancellationToken = default)
    {
        var provider = await GetServiceProviderAsync(environmentUrl, cancellationToken).ConfigureAwait(false);
        return provider.GetRequiredService<IDataverseConnectionPool>();
    }

    /// <summary>
    /// Gets the query history service for the specified environment.
    /// </summary>
    /// <param name="environmentUrl">The environment URL to connect to.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The query history service.</returns>
    public async Task<IQueryHistoryService> GetQueryHistoryServiceAsync(
        string environmentUrl,
        CancellationToken cancellationToken = default)
    {
        var provider = await GetServiceProviderAsync(environmentUrl, cancellationToken).ConfigureAwait(false);
        return provider.GetRequiredService<IQueryHistoryService>();
    }

    /// <summary>
    /// Gets the export service for the specified environment.
    /// </summary>
    /// <param name="environmentUrl">The environment URL to connect to.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The export service.</returns>
    public async Task<IExportService> GetExportServiceAsync(
        string environmentUrl,
        CancellationToken cancellationToken = default)
    {
        var provider = await GetServiceProviderAsync(environmentUrl, cancellationToken).ConfigureAwait(false);
        return provider.GetRequiredService<IExportService>();
    }

    /// <summary>
    /// Invalidates the current session, disposing the connection pool.
    /// The next call to Get*Async will create a fresh provider.
    /// </summary>
    public async Task InvalidateAsync()
    {
        await _lock.WaitAsync().ConfigureAwait(false);
        try
        {
            if (_serviceProvider != null)
            {
                await _serviceProvider.DisposeAsync().ConfigureAwait(false);
                _serviceProvider = null;
                _currentEnvironmentUrl = null;
            }
        }
        finally
        {
            _lock.Release();
        }
    }

    #region Local Services (no Dataverse connection required)

    /// <summary>
    /// Gets the profile service for profile management operations.
    /// This service uses local file storage and does not require a Dataverse connection.
    /// </summary>
    /// <returns>The profile service.</returns>
    public IProfileService GetProfileService()
    {
        return new ProfileService(_profileStore, NullLogger<ProfileService>.Instance);
    }

    /// <summary>
    /// Gets the environment service for environment discovery and selection.
    /// This service uses local file storage and does not require a Dataverse connection.
    /// </summary>
    /// <returns>The environment service.</returns>
    public IEnvironmentService GetEnvironmentService()
    {
        return new EnvironmentService(_profileStore, NullLogger<EnvironmentService>.Instance);
    }

    /// <summary>
    /// Gets the shared profile store for direct profile collection access.
    /// Prefer using <see cref="GetProfileService"/> for business operations.
    /// </summary>
    /// <returns>The shared profile store.</returns>
    public ProfileStore GetProfileStore()
    {
        return _profileStore;
    }

    /// <summary>
    /// Gets the theme service for color scheme and environment detection.
    /// </summary>
    /// <returns>The theme service.</returns>
    public ITuiThemeService GetThemeService()
    {
        return new TuiThemeService();
    }

    #endregion

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        if (_serviceProvider != null)
        {
            await _serviceProvider.DisposeAsync().ConfigureAwait(false);
            _serviceProvider = null;
        }

        _lock.Dispose();
    }
}
