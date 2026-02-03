namespace VokunModManager.Models;

public class Mod(ushort loadOrder ,string modName, bool isEnabled)
{
    public ushort LoadOrder { get; set; } = loadOrder;
    public string Name { get; set; } = modName;
    public bool IsEnabled { get; set; } = isEnabled;

    public override string ToString() => IsEnabled ? $"*{Name}" : $"{Name}";
}