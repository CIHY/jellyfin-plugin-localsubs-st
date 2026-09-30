using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Jellyfin.Plugin.LocalSubs.Mvc;

/// <summary>
/// Single template string model.
/// </summary>
public class TemplateModel
{
    /// <summary>
    /// Gets or sets a template string.
    /// </summary>
    [Required]
    public string Template { get; set; } = string.Empty;
}
