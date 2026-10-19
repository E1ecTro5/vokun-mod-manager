namespace VokunModManager.Models;

public class Plugin(string pluginName)
{
    public string Name { get; set; } = pluginName;
    public string[] Types {get; set;} = Array.Empty<string>();
}