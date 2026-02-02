namespace VokunModManager.Models;

public class Mod(string modName, bool isEnabled)
{
    public string Name { get; set; } = modName;
    public bool IsEnabled { get; set; } = isEnabled;
}