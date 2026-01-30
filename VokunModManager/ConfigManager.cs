using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;

namespace VokunModManager;

public class ConfigManager
{
    private static ConfigManager _instance;
    public static ConfigManager Instance => _instance ??= new ConfigManager();
    
    /// <summary>
    /// Path of the original Steam version of the game. All the mods should go to this path.
    /// However, they won't be active in vanilla launcher. They will launch throgh the skse64_loader.exe
    /// </summary>
    public string? OriginGamePath { get; private set; }
    
    /// <summary>
    /// Path of the skse64_loader.exe (modded) version of the game.
    /// </summary>
    public string? SkseGamePath { get; private set; }
    
    /// <summary>
    /// Path of the Plugins.txt file. All the mods (both active(*) and inactive( )) should be seen here.
    /// </summary>
    public string? PluginFilePath { get; private set; }

    public async Task SetPath(string game, string path)
    {
        switch (game)
        {
            case "origin": await SetGamePath(path); break;
            case "skse": await  SetModdedGamePath(path); break;
        }
    }
    
    private Task SetGamePath(string gamePath)
    {
        OriginGamePath = gamePath;
        return Task.CompletedTask;
    }
    
    private Task SetModdedGamePath(string gamePath)
    {
        SkseGamePath = gamePath;
        return Task.CompletedTask;
    }
}