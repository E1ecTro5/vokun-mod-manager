namespace VokunModManager.Models;

/// <summary>
/// Class for SKSE plugins
/// </summary>
/// <param name="pluginName">The name without the file format</param>
/// <param name="types">All types/formats of this current skse plugin</param>
public class SksePlugin(string pluginName, string[] types)
{
    public string Name { get; set; } = pluginName;
    public string[] Types { get; set; } = types;
    public string TypesToString =>  string.Join(", ", Types);
}