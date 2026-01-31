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
    private string modFilePath; // Plugins.txt
    private string gameFolderPath; //SKSE Steam folder

    public async Task UpdateConfig(string key, string value)
    {
        // same here, because of only 2 props switch should be enough
        switch (key)
        {
            case "modFilePath": modFilePath = value; break;
            case "gameFolderPath": gameFolderPath = value; break;
            default: return; // return if not match
        }
        
        await LogManager.Instance.Log($"Updated config for {key} with value: {value}");
        await ReWriteConfig(); // don't forget to update
    }

    public async Task<string[]> GetPathStrings()
    {
        return new []{ modFilePath, gameFolderPath };
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
                case "modFilePath": modFilePath = value; break;
                case "gameFolderPath": gameFolderPath = value; break;
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
            await sw.WriteLineAsync($"gameFolderPath={gameFolderPath}");
            await sw.WriteLineAsync($"modFilePath={modFilePath}");
        }
        await LogManager.Instance.Log("Config has been written.");
    }
}