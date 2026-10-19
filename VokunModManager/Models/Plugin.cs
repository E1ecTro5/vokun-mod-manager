namespace VokunModManager.Models;

public class Plugin(string pluginName, string[] types)
{
    public string Name { get; set; } = pluginName;
    public string[] Types { get; set; } = types;
    public string TypesToString =>  string.Join(", ", Types);
}