using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace PPDS.Auth.Profiles;

/// <summary>
/// Collection of authentication profiles with active profile tracking.
/// </summary>
/// <remarks>
/// <para>Schema v2 changes from v1:</para>
/// <list type="bullet">
/// <item><description>Profiles stored as array instead of dictionary</description></item>
/// <item><description>Active profile tracked by name instead of index</description></item>
/// <item><description>Secrets moved to secure credential store</description></item>
/// </list>
/// </remarks>
public sealed class ProfileCollection
{
    /// <summary>
    /// Storage format version. v2 uses array storage and name-based active profile.
    /// </summary>
    [JsonPropertyName("version")]
    public int Version { get; set; } = 2;

    /// <summary>
    /// Gets or sets the name of the active profile.
    /// Null if no profile is active (collection empty).
    /// </summary>
    [JsonPropertyName("activeProfile")]
    public string? ActiveProfileName { get; set; }

    /// <summary>
    /// Gets or sets the profiles list.
    /// </summary>
    [JsonPropertyName("profiles")]
    public List<AuthProfile> Profiles { get; set; } = new();

    /// <summary>
    /// Gets the active profile, or null if none is active.
    /// </summary>
    [JsonIgnore]
    public AuthProfile? ActiveProfile
    {
        get
        {
            if (string.IsNullOrWhiteSpace(ActiveProfileName))
                return Profiles.FirstOrDefault();

            return GetByName(ActiveProfileName) ?? Profiles.FirstOrDefault();
        }
    }

    /// <summary>
    /// Gets all profiles in index order.
    /// </summary>
    [JsonIgnore]
    public IEnumerable<AuthProfile> All => Profiles.OrderBy(p => p.Index);

    /// <summary>
    /// Gets the count of profiles.
    /// </summary>
    [JsonIgnore]
    public int Count => Profiles.Count;

    /// <summary>
    /// Gets the next available index.
    /// </summary>
    [JsonIgnore]
    public int NextIndex => Profiles.Count == 0 ? 1 : Profiles.Max(p => p.Index) + 1;

    /// <summary>
    /// Adds a profile to the collection.
    /// </summary>
    /// <param name="profile">The profile to add.</param>
    /// <param name="setAsActive">Whether to set this as the active profile.</param>
    /// <exception cref="InvalidOperationException">If a profile with the same index already exists.</exception>
    public void Add(AuthProfile profile, bool setAsActive = false)
    {
        if (profile.Index <= 0)
        {
            profile.Index = NextIndex;
        }

        if (Profiles.Any(p => p.Index == profile.Index))
        {
            throw new InvalidOperationException($"Profile with index {profile.Index} already exists.");
        }

        Profiles.Add(profile);

        // Auto-select first profile as active, or if explicitly requested
        if (setAsActive || Profiles.Count == 1)
        {
            ActiveProfileName = profile.Name;
        }
    }

    /// <summary>
    /// Gets a profile by index.
    /// </summary>
    /// <param name="index">The profile index.</param>
    /// <returns>The profile, or null if not found.</returns>
    public AuthProfile? GetByIndex(int index)
    {
        return Profiles.FirstOrDefault(p => p.Index == index);
    }

    /// <summary>
    /// Gets a profile by name (case-insensitive).
    /// </summary>
    /// <param name="name">The profile name.</param>
    /// <returns>The profile, or null if not found.</returns>
    public AuthProfile? GetByName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return null;

        return Profiles.FirstOrDefault(p =>
            string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Gets a profile by name or index string.
    /// </summary>
    /// <param name="nameOrIndex">The profile name or index (as string).</param>
    /// <returns>The profile, or null if not found.</returns>
    public AuthProfile? GetByNameOrIndex(string nameOrIndex)
    {
        if (string.IsNullOrWhiteSpace(nameOrIndex))
            return null;

        // Try as index first
        if (int.TryParse(nameOrIndex, out var index))
        {
            return GetByIndex(index);
        }

        // Try as name
        return GetByName(nameOrIndex);
    }

    /// <summary>
    /// Removes a profile by index.
    /// </summary>
    /// <param name="index">The profile index.</param>
    /// <returns>True if removed, false if not found.</returns>
    public bool RemoveByIndex(int index)
    {
        var profile = GetByIndex(index);
        if (profile == null)
        {
            return false;
        }

        Profiles.Remove(profile);

        // If we removed the active profile, select the first remaining profile
        if (string.Equals(ActiveProfileName, profile.Name, StringComparison.OrdinalIgnoreCase))
        {
            ActiveProfileName = Profiles.FirstOrDefault()?.Name;
        }

        return true;
    }

    /// <summary>
    /// Removes a profile by name.
    /// </summary>
    /// <param name="name">The profile name.</param>
    /// <returns>True if removed, false if not found.</returns>
    public bool RemoveByName(string name)
    {
        var profile = GetByName(name);
        return profile != null && RemoveByIndex(profile.Index);
    }

    /// <summary>
    /// Sets the active profile by index.
    /// </summary>
    /// <param name="index">The profile index.</param>
    /// <exception cref="InvalidOperationException">If profile not found.</exception>
    public void SetActiveByIndex(int index)
    {
        var profile = GetByIndex(index);
        if (profile == null)
        {
            throw new InvalidOperationException($"Profile with index {index} not found.");
        }

        ActiveProfileName = profile.Name;
    }

    /// <summary>
    /// Sets the active profile by name.
    /// </summary>
    /// <param name="name">The profile name.</param>
    /// <exception cref="InvalidOperationException">If profile not found.</exception>
    public void SetActiveByName(string name)
    {
        var profile = GetByName(name);
        if (profile == null)
        {
            throw new InvalidOperationException($"Profile with name '{name}' not found.");
        }

        ActiveProfileName = profile.Name;
    }

    /// <summary>
    /// Clears all profiles.
    /// </summary>
    public void Clear()
    {
        Profiles.Clear();
        ActiveProfileName = null;
    }

    /// <summary>
    /// Checks if a profile name is already in use (case-insensitive).
    /// </summary>
    /// <param name="name">The name to check.</param>
    /// <param name="excludeIndex">Optional index to exclude from check (for rename).</param>
    /// <returns>True if the name is in use.</returns>
    public bool IsNameInUse(string name, int? excludeIndex = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            return false;

        return Profiles.Any(p =>
            string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase) &&
            p.Index != excludeIndex);
    }

    /// <summary>
    /// Creates a deep copy of this collection with all profiles cloned.
    /// </summary>
    public ProfileCollection Clone()
    {
        var copy = new ProfileCollection
        {
            Version = Version,
            ActiveProfileName = ActiveProfileName
        };

        foreach (var profile in Profiles)
        {
            copy.Profiles.Add(profile.Clone());
        }

        return copy;
    }
}
