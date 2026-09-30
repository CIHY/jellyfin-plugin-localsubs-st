using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Jellyfin.Plugin.LocalSubs.Configuration;
using MediaBrowser.Model.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Jellyfin.Plugin.LocalSubs.Mvc;

/// <summary>
/// Controller for configuration page.
/// </summary>
/// <remarks>library part.</remarks>
public partial class LocalSubsController
{
    private static readonly CollectionTypeOptions[] _supportedCollectionTypes = [CollectionTypeOptions.movies, CollectionTypeOptions.tvshows, CollectionTypeOptions.homevideos, CollectionTypeOptions.mixed];

    /// <summary>
    /// Get library settings list.
    /// </summary>
    /// <response code="200">Library settings list.</response>
    /// <returns>
    /// An <see cref="NoContentResult"/> if the login info is valid, a <see cref="BadRequestResult"/> if the request body missing is data
    /// or <see cref="UnauthorizedResult"/> if the login info is not valid.
    /// </returns>
    [HttpGet("Jellyfin.Plugin.LocalSubs/GetLibrariesSettings")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public IActionResult GetLibrariesSettings()
    {
        var sourceList = GetLibrariesList();
        var configure = _pluginInstance.Configuration;

        // Delete outdated configurations
        var removeList = configure.LibrariesSettings.Where(w => !sourceList.Any(i => i.LibraryId == w.LibraryId)).ToList();
        foreach (var item in removeList)
        {
            configure.RemoveLibrarySettings(item.LibraryId);
        }

        if (removeList.Count > 0)
        {
            _pluginInstance.UpdateConfiguration(configure);
        }

        // Mix and output
        var outputList = sourceList
            .Where(w => !configure.LibrariesSettings.Any(i => i.LibraryId == w.LibraryId))
            .ToList();
        outputList.AddRange(
            configure.LibrariesSettings.Select(f => new LibrarySettingsViewModel(f)
            {
                LibraryName = sourceList.FirstOrDefault(w => w.LibraryId == f.LibraryId)?.LibraryName ?? "<Name Missed>"
            }));

        return Ok(outputList.OrderBy(f => f.LibraryId).ToList());
    }

    /// <summary>
    /// Enable or disable library settings.
    /// </summary>
    /// <response code="200">Library settings have been changed.</response>
    /// <response code="400">Library id is missing or invalid.</response>
    /// <param name="body">The request body.</param>
    /// <returns>
    /// An <see cref="NoContentResult"/> if the login info is valid, a <see cref="BadRequestResult"/> if the request body missing is data
    /// or <see cref="UnauthorizedResult"/> if the login info is not valid.
    /// </returns>
    [HttpPost("Jellyfin.Plugin.LocalSubs/UpdateLibrarySettings")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public IActionResult UpdateLibrarySettings([FromBody] LibrarySettings body)
    {
        if (body.LibraryId == Guid.Empty)
        {
            return BadRequest();
        }

        var configure = _pluginInstance.Configuration;
        if (!GetLibrariesList().Any(i => i.LibraryId == body.LibraryId))
        {
            // Delete outdated configurations
            if (configure.RemoveLibrarySettings(body.LibraryId))
            {
                _pluginInstance.UpdateConfiguration(configure);
            }

            return BadRequest("Specified library settings dose not exist.");
        }

        configure.AddOrUpdateLibrarySettings(body);
        _pluginInstance.UpdateConfiguration(configure);
        return Ok();
    }

    private List<LibrarySettingsViewModel> GetLibrariesList()
    {
        return _libraryManager.GetVirtualFolders(false)
            .Where(w => _supportedCollectionTypes.Any(i => i == w.CollectionType))
            .Select(f => new LibrarySettingsViewModel
            {
                LibraryId = Guid.Parse(f.ItemId),
                LibraryName = f.Name
            })
            .ToList();
    }
}
