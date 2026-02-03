using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Animation;
using VokunModManager.Misc;

namespace VokunModManager;

public sealed class AppConfig
{
    private static AppConfig? _instance;
    public static AppConfig Instance
    {
        get
        {
            _instance ??= new AppConfig();
            return _instance;
        }
    }

    private AppConfig()
    {
        BaseDirectory = GetRootByFile(AppDomain.CurrentDomain.BaseDirectory);
        _appConfigPath = Path.Combine(BaseDirectory, "config.txt");
    }

    public enum ConfigType
    {
        GameFolderPath,
        PluginFilePath,
        VdfConfigPath,
    }
    
    public string BaseDirectory { get; private set; } // ../VokunModManager directory
    private readonly string _appConfigPath; // .../VokunModManager/appConfig.txt ; will store it in .txt for now

    public string GameFolderPath { get; private set; } // Skyrim Steam folder
    public string PluginFilePath { get; private set; } // path for Plugins.txt
    public string VdfConfigPath { get; private set; } // shortcuts.vdf file path ; this is used to get non-steam game ID
    public ulong CompatdataFolderId { get; private set; } // ID of skse64 compatdata folder
    public ulong GameId { get; private set; } // the actual game (modded version) ID

    public async Task UpdateConfig(ConfigType key, string value)
    {
        switch (key)
        {
            case ConfigType.GameFolderPath:
                GameFolderPath = value;
                break;
            case ConfigType.PluginFilePath:
                PluginFilePath = value;
                CompatdataFolderId = await GetCompatdataId(); // automatically calculates while setting the gameFolderPath
                GameId = await GetGameId(PluginFilePath);
                break;
            case ConfigType.VdfConfigPath:
                VdfConfigPath = value;
                break;
            default: return; // return if not match
        }
        
        // maybe you should do this in set inside the props
        await LogManager.Instance.Log($"Updated config for {key} with value: {value}");
        await ReWriteConfig(); // don't forget to update
    }
    
    public async Task InitConfig()
    {
        if (!File.Exists(_appConfigPath))
            await using (File.Create(_appConfigPath)) { } // DON'T FORGET TO CLOSE THE STREAM! USE using

        var lines = await File.ReadAllLinesAsync(_appConfigPath);
        Dictionary<string,string> dict = new();

        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line) || !line.Contains('=')) continue;

            var parts = line.Split('=', 2); // 2 parts only
            var key = parts[0].Trim();
            var value = parts[1].Trim();

            // I guess this is better, because we don't need a lot of strings
            switch (key)
            {
                case "pluginFilePath": PluginFilePath = value; break;
                case "gameFolderPath": GameFolderPath = value; break;
                case "vdfConfigPath": VdfConfigPath = value; break;
                
                case "compatdataFolderId": CompatdataFolderId = Convert.ToUInt64(value); break;
                case "gameId": GameId = Convert.ToUInt64(value); break;
                default: continue; // skip if not match
            }

            await LogManager.Instance.Log($"Path for {key} initialized with value: {value}");
        }
        
        await LogManager.Instance.Log("Config initialized");
    }

    // this should be activated only once per startup
    private static string GetRootByFile(string startPath)
    {
        var dir = new DirectoryInfo(startPath);
        while (dir != null)
        {
            if (dir.GetFiles("VokunModManager.sln").Any()) 
                return dir.FullName;
            dir = dir.Parent;
        }
        return startPath;
    }
    private async Task ReWriteConfig()
    {
        await using (StreamWriter sw = new StreamWriter(_appConfigPath))
        {
            // GAME PATH GOES FIRST ; MODFILE (Plugins.txt) GOES SECOND
            await sw.WriteLineAsync($"gameFolderPath={GameFolderPath}");
            await sw.WriteLineAsync($"pluginFilePath={PluginFilePath}");
            await sw.WriteLineAsync($"vdfConfigPath={VdfConfigPath}");
            
            await sw.WriteLineAsync($"compatdataFolderId={CompatdataFolderId}");
            await sw.WriteLineAsync($"gameId={GameId}");
        }
        await LogManager.Instance.Log("Config has been written.");
    }

    // maybe remove method from this class?
    private async Task<ulong> GetCompatdataId()
    {
        DirectoryInfo dir = new DirectoryInfo(PluginFilePath);
        
        // find the pfx dir and get its ID
        while (!dir.Name.Equals("pfx"))
        {
            dir = dir.Parent;
        }

        // we expect to get an ID -> ../{ID}/pfx/...
        //modGameSteamId = Convert.ToUInt64(dir.Parent.Name);
        // bad practice?
        string result = dir.Parent.Name;
        await LogManager.Instance.Log($"Updated config for GameID with value: {result}");
        return Convert.ToUInt64(result);
    }

    private async Task<ulong> GetGameId(string pluginPath)
    {
        string path = Path.Combine(PluginFilePath, "skse64_loader.exe");
        return GameIdFinder.GetLongId(path);
    }
}