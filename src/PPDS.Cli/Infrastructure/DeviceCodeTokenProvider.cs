using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Extensions.Msal;

namespace PPDS.Cli.Infrastructure;

/// <summary>
/// Provides OAuth tokens using device code flow for CLI interactive authentication.
/// Tokens are cached to disk so users don't need to re-authenticate every command.
/// </summary>
/// <remarks>
/// <para>
/// Uses MSAL's cross-platform token cache with platform-specific encryption:
/// </para>
/// <list type="bullet">
///   <item><b>Windows</b>: DPAPI encryption</item>
///   <item><b>macOS</b>: Keychain</item>
///   <item><b>Linux</b>: libsecret/Secret Service (with plaintext fallback)</item>
/// </list>
/// <para>
/// Cache location: <c>%LOCALAPPDATA%\PPDS\</c> (Windows) or <c>~/.ppds/</c> (Linux/macOS)
/// </para>
/// </remarks>
public sealed class DeviceCodeTokenProvider
{
    /// <summary>
    /// Microsoft's well-known public client ID for development/prototyping with Dataverse.
    /// See: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/xrm-tooling/use-connection-strings-xrm-tooling-connect
    /// </summary>
    private const string MicrosoftPublicClientId = "51f81489-12ee-4a9e-aaae-a2591f45987d";

    /// <summary>
    /// The Dataverse scope for user impersonation.
    /// The {url}/.default requests all configured permissions for the app.
    /// </summary>
    private const string DataverseScopeTemplate = "{0}/.default";

    /// <summary>
    /// Cache file name.
    /// </summary>
    private const string CacheFileName = "msal_token_cache.bin";

    /// <summary>
    /// Application name for cache directory.
    /// </summary>
    private const string AppName = "PPDS";

    private readonly IPublicClientApplication _msalClient;
    private readonly string _dataverseUrl;
    private readonly string[] _scopes;
    private MsalCacheHelper? _cacheHelper;
    private bool _cacheInitialized;
    private AuthenticationResult? _cachedToken;

    /// <summary>
    /// Creates a new device code token provider for the specified Dataverse URL.
    /// </summary>
    /// <param name="dataverseUrl">The Dataverse environment URL.</param>
    public DeviceCodeTokenProvider(string dataverseUrl)
    {
        _dataverseUrl = dataverseUrl.TrimEnd('/');
        _scopes = [string.Format(DataverseScopeTemplate, _dataverseUrl)];

        _msalClient = PublicClientApplicationBuilder
            .Create(MicrosoftPublicClientId)
            .WithAuthority(AzureCloudInstance.AzurePublic, "common")
            .WithDefaultRedirectUri()
            .Build();
    }

    /// <summary>
    /// Gets an access token for the Dataverse instance.
    /// Uses cached token if available and not expired, otherwise initiates device code flow.
    /// </summary>
    /// <param name="instanceUri">The Dataverse instance URI (passed by ServiceClient).</param>
    /// <returns>The access token.</returns>
    public async Task<string> GetTokenAsync(string instanceUri)
    {
        // Initialize persistent cache on first call
        await EnsureCacheInitializedAsync();

        // Try to get token silently from in-memory cache first
        if (_cachedToken != null && _cachedToken.ExpiresOn > DateTimeOffset.UtcNow.AddMinutes(5))
        {
            return _cachedToken.AccessToken;
        }

        // Try silent acquisition (from MSAL's persistent cache)
        var accounts = await _msalClient.GetAccountsAsync();
        var account = accounts.FirstOrDefault();

        if (account != null)
        {
            try
            {
                _cachedToken = await _msalClient
                    .AcquireTokenSilent(_scopes, account)
                    .ExecuteAsync();
                return _cachedToken.AccessToken;
            }
            catch (MsalUiRequiredException)
            {
                // Silent acquisition failed, need interactive
            }
        }

        // Fall back to device code flow
        _cachedToken = await _msalClient
            .AcquireTokenWithDeviceCode(_scopes, deviceCodeResult =>
            {
                // Display the device code message to the user
                Console.WriteLine();
                Console.WriteLine("To sign in, use a web browser to open the page:");
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine($"  {deviceCodeResult.VerificationUrl}");
                Console.ResetColor();
                Console.WriteLine();
                Console.WriteLine("Enter the code:");
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"  {deviceCodeResult.UserCode}");
                Console.ResetColor();
                Console.WriteLine();
                Console.WriteLine("Waiting for authentication...");
                return Task.CompletedTask;
            })
            .ExecuteAsync();

        Console.WriteLine($"Authenticated as: {_cachedToken.Account.Username}");
        Console.WriteLine();

        return _cachedToken.AccessToken;
    }

    /// <summary>
    /// Initializes the persistent token cache.
    /// </summary>
    private async Task EnsureCacheInitializedAsync()
    {
        if (_cacheInitialized)
            return;

        try
        {
            var cacheDir = GetCacheDirectory();
            Directory.CreateDirectory(cacheDir);

            var storageProperties = new StorageCreationPropertiesBuilder(CacheFileName, cacheDir)
                .WithUnprotectedFile() // Fallback for Linux without libsecret
                .Build();

            _cacheHelper = await MsalCacheHelper.CreateAsync(storageProperties);
            _cacheHelper.RegisterCache(_msalClient.UserTokenCache);

            _cacheInitialized = true;
        }
        catch (MsalCachePersistenceException ex)
        {
            // Cache persistence failed - continue without persistent cache
            // User will need to re-authenticate each session but CLI will still work
            Console.Error.WriteLine($"Warning: Token cache persistence unavailable ({ex.Message}). You may need to re-authenticate each session.");
            _cacheInitialized = true; // Don't retry
        }
    }

    /// <summary>
    /// Gets the cache directory path based on the platform.
    /// </summary>
    private static string GetCacheDirectory()
    {
        if (OperatingSystem.IsWindows())
        {
            // Windows: %LOCALAPPDATA%\PPDS
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            return Path.Combine(localAppData, AppName);
        }
        else
        {
            // Linux/macOS: ~/.ppds
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            return Path.Combine(home, $".{AppName.ToLowerInvariant()}");
        }
    }
}
