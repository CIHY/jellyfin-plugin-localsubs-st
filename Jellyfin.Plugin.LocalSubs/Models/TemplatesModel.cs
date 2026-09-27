using System;
using System.Collections.Generic;
using System.Text;

namespace Jellyfin.Plugin.LocalSubs;

/// <summary>
/// Multiple template strings model.
/// </summary>
public class TemplatesModel
{
    /// <summary>
    /// Gets or sets a template string.
    /// </summary>
    public IEnumerable<string> Templates { get; set; } = new List<string>();
}
