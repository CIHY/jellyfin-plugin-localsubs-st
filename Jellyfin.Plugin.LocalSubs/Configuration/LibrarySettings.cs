#pragma warning disable CA1819

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace Jellyfin.Plugin.LocalSubs.Configuration;

/// <summary>
/// Library setting.
/// </summary>
public class LibrarySettings
{
    private readonly List<string> _isoSubtitleLangs = new List<string>();

    /// <summary>
    /// Gets or sets library id.
    /// </summary>
    public Guid LibraryId { get; set; } = Guid.Empty;

    /// <summary>
    /// Gets or sets the language of the subtitle file.
    /// </summary>
    public string[] SubtitleLangsISO
    {
        get
        {
            return _isoSubtitleLangs.ToArray();
        }

        set
        {
            _isoSubtitleLangs.Clear();
            if (value is null || value.Length == 0)
            {
                return;
            }

            _isoSubtitleLangs.AddRange(value.Where(w => !string.IsNullOrEmpty(w)));
        }
    }

    /// <summary>
    /// Gets or sets a value indicating whether to skip files with embedded subtitles; the default value is true.
    /// </summary>
    public bool SkipIfHaveEmbedded { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to skip files that have matching language audio tracks; the default value is true.
    /// </summary>
    public bool SkipIfHaveMatchingAudioTracks { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether local subtitle search is enabled; the default value is false.
    /// </summary>
    public bool IsEnabled { get; set; } = false;

    /// <summary>
    /// Gets a value indicating whether local subtite search can be performed.
    /// </summary>
    [XmlIgnore]
    public bool CanExecSearch => IsEnabled && _isoSubtitleLangs.Count > 0;
}
