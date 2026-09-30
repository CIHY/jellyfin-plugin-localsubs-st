using System.Net.Mime;
using MediaBrowser.Common.Api;
using MediaBrowser.Controller.Library;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Jellyfin.Plugin.LocalSubs.Mvc;

/// <summary>
/// Controller for configuration page.
/// </summary>
/// <remarks>template/main part.</remarks>
[ApiController]
[Produces(MediaTypeNames.Application.Json)]
[Authorize(Policy = Policies.SubtitleManagement)]
public partial class LocalSubsController : ControllerBase
{
    private readonly LocalSubsPlugin _pluginInstance;
    private readonly ILibraryManager _libraryManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="LocalSubsController"/> class.
    /// </summary>
    /// <param name="pluginInstance">Instance of the <see cref="LocalSubsPlugin"/> class.</param>
    /// <param name="libraryManager">Instance of the <see cref="ILibraryManager"/> class.</param>
    public LocalSubsController(LocalSubsPlugin pluginInstance, ILibraryManager libraryManager)
    {
        _pluginInstance = pluginInstance;
        _libraryManager = libraryManager;
    }

    /// <summary>
    /// Add template string.
    /// </summary>
    /// <response code="200">Template string valid.</response>
    /// <response code="400">Template string is missing or invalid.</response>
    /// <param name="body">The request body.</param>
    /// <returns>
    /// An <see cref="NoContentResult"/> if the login info is valid, a <see cref="BadRequestResult"/> if the request body missing is data
    /// or <see cref="UnauthorizedResult"/> if the login info is not valid.
    /// </returns>
    [HttpPost("Jellyfin.Plugin.LocalSubs/AddTemplate")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public IActionResult AddTemplate([FromBody] TemplateModel body)
    {
        string template = body.Template.Trim();
        if (string.IsNullOrEmpty(template))
        {
            return BadRequest();
        }

        _pluginInstance.Configuration.AddTemplate(template);
        _pluginInstance.UpdateConfiguration(_pluginInstance.Configuration);
        return Ok();
    }

    /// <summary>
    /// Remove template strings.
    /// </summary>
    /// <response code="200">Template string valid.</response>
    /// <response code="400">Template string is missing or invalid.</response>
    /// <param name="body">The request body.</param>
    /// <returns>
    /// An <see cref="NoContentResult"/> if the login info is valid, a <see cref="BadRequestResult"/> if the request body missing is data
    /// or <see cref="UnauthorizedResult"/> if the login info is not valid.
    /// </returns>
    [HttpPost("Jellyfin.Plugin.LocalSubs/DeleteTemplates")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public IActionResult DeleteTemplates([FromBody] string[] body)
    {
        if (body.Length == 0)
        {
            return BadRequest();
        }

        foreach (string template in body)
        {
            if (!string.IsNullOrEmpty(template))
            {
                _pluginInstance.Configuration.RemoveTemplate(template);
            }
        }

        _pluginInstance.UpdateConfiguration(_pluginInstance.Configuration);
        return Ok();
    }

    /// <summary>
    /// Reset template strings to defaults.
    /// </summary>
    /// <response code="200">Reset successful.</response>
    /// <returns>
    /// An <see cref="NoContentResult"/> if the login info is valid, a <see cref="BadRequestResult"/> if the request body missing is data
    /// or <see cref="UnauthorizedResult"/> if the login info is not valid.
    /// </returns>
    [HttpGet("Jellyfin.Plugin.LocalSubs/GetTemplates")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public ActionResult GetTemplates()
    {
        return Ok(_pluginInstance.Configuration.Templates);
    }

    /// <summary>
    /// Reset template strings to defaults.
    /// </summary>
    /// <response code="200">Reset successful.</response>
    /// <returns>
    /// An <see cref="NoContentResult"/> if the login info is valid, a <see cref="BadRequestResult"/> if the request body missing is data
    /// or <see cref="UnauthorizedResult"/> if the login info is not valid.
    /// </returns>
    [HttpPost("Jellyfin.Plugin.LocalSubs/ResetTemplates")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public ActionResult ResetTemplates()
    {
        _pluginInstance.Configuration.ResetTemplates();
        _pluginInstance.UpdateConfiguration(_pluginInstance.Configuration);
        return Ok();
    }
}
