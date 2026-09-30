#pragma warning disable CA1819

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.LocalSubs.Configuration;

/// <summary>Plugin configuration.</summary>
/// <remarks>template/main part.</remarks>
[Serializable]
public partial class LocalSubsConfiguration : BasePluginConfiguration
{
    private List<string> _templates;

    /// <summary>
    /// Initializes a new instance of the <see cref="LocalSubsConfiguration"/> class.
    /// </summary>
    public LocalSubsConfiguration()
    {
        _templates = [];
        _librariesSettings = [];
    }

    /// <summary>Gets or sets template strings.</summary>
    public string[] Templates
    {
        get
        {
            return _templates.ToArray();
        }

        set
        {
            _templates.Clear();
            if (value is null || value.Length == 0)
            {
                return;
            }

            _templates.AddRange(value.Where(w => !string.IsNullOrEmpty(w)));
        }
    }

    /// <summary>Adds a new template.</summary>
    /// <param name="template">Template string.</param>
    public void AddTemplate(string template)
    {
        if (!_templates.Contains(template))
        {
            _templates.Add(template);
        }
    }

    /// <summary>Remove a template string.</summary>
    /// <param name="template">Template string.</param>
    public void RemoveTemplate(string template)
    {
        _templates.Remove(template);
    }

    /// <summary>Reset templates to default.</summary>
    public void ResetTemplates()
    {
        _templates.Clear();
        _templates.Add(Path.Join("Subs", "%fn%", "%n%_%l%.srt"));
        _templates.Add(Path.Join("Subs", "%fn%.%l%.srt"));
        _templates.Add(Path.Join("Subs", "%n%_%l%.srt"));
        _templates.Add(Path.Join("Subs", "%l%.srt"));
        _templates.Add(Path.Join("Subs", "%fn%.srt"));
    }
}
