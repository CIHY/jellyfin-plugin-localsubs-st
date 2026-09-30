#pragma warning disable CS1591

using Jellyfin.Plugin.LocalSubs.Configuration;

namespace Jellyfin.Plugin.LocalSubs.Mvc
{
    public class LibrarySettingsViewModel : LibrarySettings
    {
        public LibrarySettingsViewModel()
        {
        }

        public LibrarySettingsViewModel(LibrarySettings source)
        {
            LibraryId = source.LibraryId;
            SubtitleLangsISO = source.SubtitleLangsISO;
            IsEnabled = source.IsEnabled;
            SkipIfHaveEmbedded = source.SkipIfHaveEmbedded;
            SkipIfHaveMatchingAudioTracks = source.SkipIfHaveMatchingAudioTracks;
        }

        public string LibraryName { get; set; } = string.Empty;
    }
}
