#nullable disable

#pragma warning disable CS1591
#pragma warning disable CA1873

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Data.Enums;
using Jellyfin.Database.Implementations.Entities.Libraries;
using Jellyfin.Extensions;
using Jellyfin.Plugin.LocalSubs.Configuration;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Controller.Subtitles;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Globalization;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Providers;
using MediaBrowser.Model.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.LocalSubs;

public class LocalSubtitleScheduledTask : IScheduledTask
{
    private readonly ILibraryManager _libraryManager;
    private readonly ILocalizationManager _localization;
    private readonly ILibraryMonitor _monitor;
    private readonly IDirectoryService _directoryService;
    private readonly ILoggerFactory _loggerFactory;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ILogger<LocalSubtitleScheduledTask> _logger;
    private readonly HashSet<string> _allowedSubtitleFormats;

    public LocalSubtitleScheduledTask(
        ILibraryManager libraryManager,
        ILocalizationManager localization,
        ILibraryMonitor monitor,
        IDirectoryService directoryService,
        ILoggerFactory loggerFactory,
        IServiceScopeFactory serviceScopeFactory,
        Emby.Naming.Common.NamingOptions namingOptions)
    {
        _libraryManager = libraryManager;
        _localization = localization;
        _monitor = monitor;
        _directoryService = directoryService;
        _loggerFactory = loggerFactory;
        _serviceScopeFactory = serviceScopeFactory;
        _logger = loggerFactory.CreateLogger<LocalSubtitleScheduledTask>();
        _allowedSubtitleFormats = new HashSet<string>(
            namingOptions.SubtitleFileExtensions.Select(e => e.TrimStart('.')),
            StringComparer.OrdinalIgnoreCase);
    }

    public string Name => "Search for local subtitles";

    public string Key => "FindLocalSubtitles";

    public bool IsHidden => false;

    public bool IsLogged => true;

    public string Description => "Search for local subtitles for all videos.";

    public string Category => _localization.GetLocalizedString("TasksLibraryCategory");

    /// <inheritdoc />
    public async Task ExecuteAsync(IProgress<double> progress, CancellationToken cancellationToken)
    {
        using (IServiceScope serviceScope = _serviceScopeFactory.CreateScope())
        {
            await ExecuteAsyncInternal(serviceScope, progress, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task ExecuteAsyncInternal(IServiceScope serviceScope, IProgress<double> progress, CancellationToken cancellationToken)
    {
        var types = new[] { BaseItemKind.Video };
        var dict = new Dictionary<Guid, BaseItem>();
        var pluginConfigure = serviceScope.ServiceProvider.GetService<LocalSubsPlugin>().Configuration;

        foreach (var library in _libraryManager.RootFolder.Children.ToList())
        {
            TryGetLibrarySettings(library, pluginConfigure, out var librarySettings);
            if (librarySettings == null)
            {
                continue;
            }

            if (!librarySettings.CanExecSearch)
            {
                // Skip this library if local subtitle search is disabled or the search language is not configured
                _logger.LogInformation("Local subtitle search is not enabled. ::Library => [{Id}]{Path}", librarySettings.LibraryId, library.Path);
                continue;
            }

            // var libraryOptions = _libraryManager.GetLibraryOptions(library);
            string[] subtitleDownloadLanguages = librarySettings.SubtitleLangsISO; // libraryOptions.SubtitleDownloadLanguages;
            bool skipIfEmbeddedSubtitlesPresent = librarySettings.SkipIfHaveEmbedded; // libraryOptions.SkipSubtitlesIfEmbeddedSubtitlesPresent;
            bool skipIfAudioTrackMatches = librarySettings.SkipIfHaveMatchingAudioTracks; // libraryOptions.SkipSubtitlesIfAudioTrackMatches;

            foreach (var lang in subtitleDownloadLanguages)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var query = new InternalItemsQuery
                {
                    MediaTypes = new[] { MediaType.Video },
                    IsVirtualItem = false,
                    IncludeItemTypes = types,
                    DtoOptions = new DtoOptions(true),
                    SourceTypes = new[] { SourceType.Library },
                    Parent = library,
                    Recursive = true,
                    IncludeOwnedItems = true
                };

                if (skipIfAudioTrackMatches)
                {
                    query.HasNoAudioTrackWithLanguage = lang;
                }

                if (skipIfEmbeddedSubtitlesPresent)
                {
                    // Exclude if it already has any subtitles of the same language
                    query.HasNoSubtitleTrackWithLanguage = lang;
                }
                else
                {
                    // Exclude if it already has external subtitles of the same language
                    query.HasNoExternalSubtitleTrackWithLanguage = lang;
                }

                var videosByLanguage = _libraryManager.GetItemList(query);

                foreach (var video in videosByLanguage)
                {
                    dict[video.Id] = video;
                }
            }
        }

        var videos = dict.Values.ToList();
        if (videos.Count == 0)
        {
            return;
        }

        var numComplete = 0;

        _logger.LogInformation("Task will be start... {Number} video(s).", videos.Count);
        foreach (var video in videos)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                await FindLocalSubtitles(serviceScope, video as Video, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching subtitles. => {VideoPath}", video.Path);
            }

            // Update progress
            numComplete++;
            double percent = numComplete;
            percent /= videos.Count;

            progress.Report(100 * percent);
        }
    }

    private bool TryGetLibrarySettings(BaseItem lookup, LocalSubsConfiguration pluginConfigure, out LibrarySettings target)
    {
        Guid userLibraryId;
        if (lookup is CollectionFolder collectionFolder)
        {
            userLibraryId = collectionFolder.Id;
        }
        else
        {
            if (_libraryManager.GetCollectionFolders(lookup).Find(folder => folder is CollectionFolder) is CollectionFolder collectionFolder2)
            {
                userLibraryId = collectionFolder2.Id;
            }
            else
            {
                _logger.LogDebug("Unable to obtain the ID of library. Lookup => {Path}", lookup.Path);
                target = null;
                return false;
            }
        }

        target = pluginConfigure.GetLibrarySettings(userLibraryId);
        if (target == null)
        {
            target = new LibrarySettings() { LibraryId = userLibraryId };
            return false;
        }

        return true;
    }

    private async Task<bool> FindLocalSubtitles(IServiceScope serviceScope, Video video, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(video, nameof(video));

        var mediaStreams = video.GetMediaStreams();

        var pluginConfigure = serviceScope.ServiceProvider.GetService<LocalSubsPlugin>().Configuration;
        if (!TryGetLibrarySettings(video, pluginConfigure, out var librarySettings) || !librarySettings.CanExecSearch)
        {
            // Skip this library if local subtitle search is disabled or the search language is not configured
            return true;
        }

        string[] subtitleDownloadLanguages = librarySettings.SubtitleLangsISO;
        bool skipIfEmbeddedSubtitlesPresent = librarySettings.SkipIfHaveEmbedded;
        bool skipIfAudioTrackMatches = librarySettings.SkipIfHaveMatchingAudioTracks;

        if (video.VideoType != VideoType.VideoFile)
        {
            return false;
        }

        if (!video.IsCompleteMedia)
        {
            return false;
        }

        // Start search
        int downloadedLanguages = 0;
        string videoPath = video.Path ?? video.Id.ToString();

        foreach (var language in subtitleDownloadLanguages)
        {
            cancellationToken.ThrowIfCancellationRequested();
            downloadedLanguages++;

            // There's already subtitles for this language
            if (mediaStreams.Any(i => i.Type == MediaStreamType.Subtitle && i.IsTextSubtitleStream && string.Equals(i.Language, language, StringComparison.OrdinalIgnoreCase)))
            {
                _logger.LogDebug("Video has a subtitle file in the specified language, subtitle search abort. ::{Lang} => {VideoPath}", language, videoPath);
                continue;
            }

            var audioStreams = mediaStreams.Where(i => i.Type == MediaStreamType.Audio).ToList();
            var defaultAudioStreams = audioStreams.Where(i => i.IsDefault).ToList();

            // If none are marked as default, just take a guess
            if (defaultAudioStreams.Count == 0)
            {
                defaultAudioStreams = audioStreams.Take(1).ToList();
            }

            // There's already a default audio stream for this language
            if (skipIfAudioTrackMatches &&
                defaultAudioStreams.Any(i => string.Equals(i.Language, language, StringComparison.OrdinalIgnoreCase)))
            {
                _logger.LogDebug("Video has an audio stream in the specified language, subtitle search abort. ::{Lang} => {VideoPath}", language, videoPath);
                continue;
            }

            // There's an internal subtitle stream for this language
            if (skipIfEmbeddedSubtitlesPresent &&
                mediaStreams.Any(i => i.Type == MediaStreamType.Subtitle && !i.IsExternal && string.Equals(i.Language, language, StringComparison.OrdinalIgnoreCase)))
            {
                _logger.LogDebug("Video has a subtitle stream in the specified language, subtitle search abort. ::{Lang} => {VideoPath}", language, videoPath);
                continue;
            }

            var request = new SubtitleSearchRequest
            {
                Language = language,
                MediaPath = videoPath,
                Name = video.Name,
            };

            try
            {
                _logger.LogDebug("Subtitle search start. ::{Lang} => {VideoPath}", language, videoPath);
                var localSubsProvider = new LocalSubsProvider(_loggerFactory.CreateLogger<LocalSubsProvider>(), new ScopedServiceProvider(serviceScope));

                var searchResults = await SearchSubtitles(localSubsProvider, request, cancellationToken).ConfigureAwait(false);

                var result = searchResults.FirstOrDefault();

                if (result is not null)
                {
                    await TrySaveSubtitles(localSubsProvider, video, result.Id, cancellationToken).ConfigureAwait(false);

                    continue;
                }

                _logger.LogInformation("Subtitle search ok, NO MATCHS. ::{Lang} => {VideoPath}", language, videoPath);
            }
            catch (RateLimitExceededException)
            {
            }
            catch (TaskCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching subtitles. ::{Lang} => {VideoPath}", language, videoPath);
            }
        }

        // Rescan
        if (downloadedLanguages > 0)
        {
            await video.RefreshMetadata(cancellationToken).ConfigureAwait(false);
            return false;
        }

        return true;
    }

    private async Task<RemoteSubtitleInfo[]> SearchSubtitles(LocalSubsProvider localSubsProvider, SubtitleSearchRequest request, CancellationToken cancellationToken)
    {
        if (request.Language is not null)
        {
            var culture = _localization.FindLanguageInfo(request.Language);

            if (culture is not null)
            {
                request.TwoLetterISOLanguageName = culture.TwoLetterISOLanguageName;
            }
        }

        var searchResults = await localSubsProvider.Search(request, cancellationToken).ConfigureAwait(false);
        var list = searchResults.ToArray();

        if (list.Length > 0)
        {
            foreach (var sub in list)
            {
                sub.Id = sub.ProviderName.ToLowerInvariant().GetMD5().ToString("N", CultureInfo.InvariantCulture) + "_" + sub.Id;
            }

            return list;
        }

        return Array.Empty<RemoteSubtitleInfo>();
    }

    private async Task TrySaveSubtitles(LocalSubsProvider localSubsProvider, Video video, string subtitleId, CancellationToken cancellationToken)
    {
        var parts = subtitleId.Split('_', 2);
        var response = await localSubsProvider.GetSubtitles(parts[^1], cancellationToken).ConfigureAwait(false);

        MemoryStream memoryStream;
        if (response.Stream is MemoryStream)
        {
            memoryStream = (MemoryStream)response.Stream;
        }
        else
        {
            memoryStream = new MemoryStream();
            var stream = response.Stream;
            await using (stream.ConfigureAwait(false))
            {
                await stream.CopyToAsync(memoryStream, cancellationToken).ConfigureAwait(false);
                memoryStream.Position = 0;
            }
        }

        // Saving process
        await using (memoryStream.ConfigureAwait(false))
        {
            var fileExtension = response.Format.ToLowerInvariant();
            if (!_allowedSubtitleFormats.Contains(fileExtension, StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException("Invalid subtitle format: " + fileExtension);
            }

            var language = response.Language.ToLowerInvariant();
            if (language.AsSpan().IndexOfAny(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) >= 0)
            {
                throw new ArgumentException("Language contains invalid characters.");
            }

            var saveFileName = Path.GetFileNameWithoutExtension(video.Path) + "." + language;

            if (response.IsForced)
            {
                saveFileName += ".forced";
            }

            if (response.IsHearingImpaired)
            {
                saveFileName += ".sdh";
            }

            // Save to metadata folder only
            var savePathWithoutExtension = Path.GetFullPath(Path.Combine(video.GetInternalMetadataPath(), saveFileName));
            var savePath = Path.GetFullPath(savePathWithoutExtension + "." + fileExtension);

            var fileExist = File.Exists(savePath);
            var fileExistCounter = 0;
            while (fileExist)
            {
                cancellationToken.ThrowIfCancellationRequested();
                savePath = string.Format(CultureInfo.InvariantCulture, "{0}.{1}.{2}", savePathWithoutExtension, fileExistCounter, fileExtension);
            }

            _logger.LogInformation("Saving subtitles to {SavePath}. ::{Lang} => {VideoPath}", savePath, language, video.Path);

            // Start saving
            try
            {
                _monitor.ReportFileSystemChangeBeginning(savePath);

                Directory.CreateDirectory(Path.GetDirectoryName(savePath) ?? throw new InvalidOperationException("Path can't be a root directory."));

                var fileOptions = AsyncFile.WriteOptions;
                fileOptions.Mode = FileMode.CreateNew;
                fileOptions.PreallocationSize = memoryStream.Length;
                var fs = new FileStream(savePath, fileOptions);
                await using (fs.ConfigureAwait(false))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    await memoryStream.CopyToAsync(fs, CancellationToken.None).ConfigureAwait(false);
                }

                _directoryService.Invalidate(savePath);
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                _monitor.ReportFileSystemChangeComplete(savePath, false);
            }
        }
    }

    /// <inheritdoc />
    public IEnumerable<TaskTriggerInfo> GetDefaultTriggers()
    {
        return
        [
            // Every 36 hours
            // A bit longer than the default interval for the built-in tasks in Jellyfin (24 hours)
            new TaskTriggerInfo { Type = TaskTriggerInfoType.IntervalTrigger, IntervalTicks = TimeSpan.FromHours(36).Ticks }
        ];
    }
}
