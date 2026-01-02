using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Extensions.Msal;
using PPDS.Auth.Profiles;

namespace PPDS.Auth.Credentials;

/// <summary>
/// Manages token cache operations including clearing cached credentials.
/// </summary>
public static class TokenCacheManager
{
    /// <summary>
    /// Clears all MSAL token caches including platform-specific secure storage.
    /// </summary>
    /// <remarks>
    /// This method clears:
    /// - The file-based token cache
    /// - macOS Keychain entries (if applicable)
    /// - Linux keyring entries (if applicable)
    /// </remarks>
    public static async Task ClearAllCachesAsync()
    {
        // Delete the file-based token cache
        if (File.Exists(ProfilePaths.TokenCacheFile))
        {
            File.Delete(ProfilePaths.TokenCacheFile);
        }

        // Clear platform-specific secure storage (Keychain, keyring, etc.)
        try
        {
            var storageProperties = new StorageCreationPropertiesBuilder(
                    ProfilePaths.TokenCacheFileName,
                    ProfilePaths.DataDirectory)
                .WithMacKeyChain("PPDS", "TokenCache")
                .WithLinuxKeyring(
                    "com.ppds.tokencache",
                    "default",
                    "PPDS Token Cache",
                    new KeyValuePair<string, string>("app", "ppds"),
                    new KeyValuePair<string, string>("version", "1"))
                .Build();

#pragma warning disable CS0618 // Clear() is obsolete but appropriate for full logout
            var cacheHelper = await MsalCacheHelper.CreateAsync(storageProperties).ConfigureAwait(false);
            cacheHelper.Clear();
#pragma warning restore CS0618
        }
        catch
        {
            // Cache may not exist or platform doesn't support secure storage - ignore
        }
    }
}
