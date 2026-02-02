namespace VokunModManager.Models;

public class Mod
{
    public string Name { get; set; }
    public bool IsEnabled { get; set; }

    public Mod(string modName, bool isEnabled)
    {
        Name = modName;
        IsEnabled = isEnabled;
    }
}