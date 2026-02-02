using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Animation;
using VokunModManager.Misc;

namespace VokunModManager;

public class AppConfig
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
        configPath = Path.Combine(BaseDirectory, "config.txt");
    }
    
    public string BaseDirectory { get; private set; }
    
    private readonly string configPath; // .../VokunModManager/appConfig.txt ; will store it in .txt for now
    
    // change get set props soon, pls
    public string GameFolderPath; // Skyrim Steam folder ; INDEX 0
    public string ModFilePath; // path for Plugins.txt ; INDEX 1 
    public ulong ModGameSteamId; // ID for skse64_launch.exe located in steam library ; INDEX 2
    public string VdfConfigPath; // shortcuts.vdf file path ; this is used to get non-steam game ID ; INDEX 3?

    public async Task UpdateConfig(string key, string value)
    {
        switch (key)
        {
            case "modFilePath":
                ModFilePath = value;
                ModGameSteamId = await GetModGameID();   // automatically calculates while setting the gameFolderPath
                break;
            case "gameFolderPath":
                GameFolderPath = value;
                break;
            case "vdfConfigPath":
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
        if (!File.Exists(configPath))
            await using (File.Create(configPath)) { } // DON'T FORGET TO CLOSE THE STREAM! USE using

        var lines = await File.ReadAllLinesAsync(configPath);
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
                case "modFilePath": ModFilePath = value; break;
                case "gameFolderPath": GameFolderPath = value; break;
                case "modGameSteamId": ModGameSteamId = Convert.ToUInt64(value); break;
                case "vdfConfigPath": VdfConfigPath = value; break;
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
        await using (StreamWriter sw = new StreamWriter(configPath))
        {
            // GAME PATH GOES FIRST ; MODFILE (Plugins.txt) GOES SECOND
            await sw.WriteLineAsync($"gameFolderPath={GameFolderPath}");
            await sw.WriteLineAsync($"modFilePath={ModFilePath}");
            await sw.WriteLineAsync($"modGameSteamId={ModGameSteamId}");
            await sw.WriteLineAsync($"vdfConfigPath={VdfConfigPath}");
        }
        await LogManager.Instance.Log("Config has been written.");
    }

    // maybe remove method from this class?
    private async Task<ulong> GetModGameID()
    {
        DirectoryInfo dir = new DirectoryInfo(ModFilePath);
        
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
}