using System.Text.Json;
using VokunModManager.Interfaces;
using VokunModManager.Models;

namespace VokunModManager.Utils;

public class ModManifestManager : IModManifestManager
{
    private readonly string _modsFolderPath;
    private readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true }; // pretty printing

    public ModManifestManager(IAppConfig config)
    {
        _modsFolderPath = config.ModsFolderPath;
    }

    public async Task WriteModFiles(Mod mod)
    {
        // format paths
        string jsonFilePath = Path.Combine(_modsFolderPath, $"{mod.Name}.json");
        await using FileStream fs = new FileStream(jsonFilePath, FileMode.Create, FileAccess.Write, FileShare.None);

        // update json path
        mod.PathToJson = jsonFilePath;
        
        // serialize into the stream
        await JsonSerializer.SerializeAsync(fs, mod, _jsonOptions);
    }
}