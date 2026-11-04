namespace VokunModManager.Models;

/// <summary>
/// Main class for 'Mod' objects. Basically, represents object from extracted archive, with list of installed files
/// </summary>
/// <param name="name">Name of the archive, which has been initialized from</param>
public class Mod(string name)
{
    public string Name { get; set; } = name;
    //public string[]? ImagePaths { get; set; } maybe not this time
    
    // these two should work together
    public string PathToJson { get; set; } = string.Empty;
    public List<string> InstalledFiles { get; set; } = new();
}