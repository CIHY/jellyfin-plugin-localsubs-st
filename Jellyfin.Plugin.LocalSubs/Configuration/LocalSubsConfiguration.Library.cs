#pragma warning disable CA1819

using System;
using System.Collections.Generic;
using System.Linq;

namespace Jellyfin.Plugin.LocalSubs.Configuration;

/// <summary>Plugin configuration.</summary>
/// <remarks>library settings part.</remarks>
public partial class LocalSubsConfiguration
{
    private readonly List<LibrarySettings> _librariesSettings;

    /// <summary>
    /// Gets or sets libraries settings.
    /// </summary>
    public LibrarySettings[] LibrariesSettings
    {
        get
        {
            return _librariesSettings.ToArray();
        }

        set
        {
            _librariesSettings.Clear();
            if (value is null || value.Length == 0)
            {
                return;
            }

            _librariesSettings.AddRange(value.Where(w => w.LibraryId != Guid.Empty));
        }
    }

    /// <summary>
    /// Add or update library settings.
    /// </summary>
    /// <param name="source">Library settings.</param>
    public void AddOrUpdateLibrarySettings(LibrarySettings source)
    {
        if (TryGetLibrarySettings(source.LibraryId, out LibrarySettings target))
        {
            target.SubtitleLangsISO = source.SubtitleLangsISO;
            target.IsEnabled = source.IsEnabled;
            target.SkipIfHaveEmbedded = source.SkipIfHaveEmbedded;
            target.SkipIfHaveMatchingAudioTracks = source.SkipIfHaveMatchingAudioTracks;
            return;
        }

        _librariesSettings.Add(source);
    }

    /// <summary>
    /// Remove library settings.
    /// </summary>
    /// <param name="id">Library id.</param>
    /// <returns>true if the specified library setting has been removed; otherwise is false (the library setting dose not exist).</returns>
    public bool RemoveLibrarySettings(Guid id)
    {
        var itemIdx = _librariesSettings.FindIndex(match => match.LibraryId == id);
        if (itemIdx >= 0)
        {
            _librariesSettings.RemoveAt(itemIdx);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Check whether the specified library has enabled local subtitle search.
    /// </summary>
    /// <param name="id">Library id.</param>
    /// <returns>true if the library has enabled local subtitle search and has specified the subtitle languages; otherwise false.</returns>
    public bool LibraryEnabledLocalSubtitleSearch(Guid id)
    {
        return TryGetLibrarySettings(id, out var target) && target.IsEnabled && target.SubtitleLangsISO.Length > 0;
    }

    /// <summary>
    /// Get specified library settings.
    /// </summary>
    /// <param name="id">Library id.</param>
    /// <returns>Library settings, it will be null.</returns>
    public LibrarySettings? GetLibrarySettings(Guid id)
    {
        if (TryGetLibrarySettings(id, out var target))
        {
            return target;
        }

        return null;
    }

    /// <summary>
    /// Check whether the specified library settings exist.
    /// </summary>
    /// <param name="id">Library id.</param>
    /// <returns>true if the library settings exist; otherwise false.</returns>
    public bool LibrarySettingsExists(Guid id)
    {
        return TryGetLibrarySettings(id, out _);
    }

    private bool TryGetLibrarySettings(Guid id, out LibrarySettings target)
    {
        var itemIdx = _librariesSettings.FindIndex(match => match.LibraryId == id);
        if (itemIdx >= 0)
        {
            target = _librariesSettings[itemIdx];
            return true;
        }

        target = new LibrarySettings();
        return false;
    }
}
