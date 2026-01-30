using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Animation;

namespace VokunModManager;

public class AppConfig
{
    private static AppConfig? _instance;
    public static AppConfig Instance
    {
        get
        {
            if (_instance == null)
                _instance = new AppConfig();
            return _instance;
        }
    }

    private AppConfig()
    {
        BaseDirectory = GetRootByFile(AppDomain.CurrentDomain.BaseDirectory);
        configPath = Path.Combine(BaseDirectory, "config.txt");
        
        InitConfig().Wait();
    }
    
    public string BaseDirectory { get; private set; }
    public Dictionary<string, string> ConfigStates { get; private set; }
    private readonly string configPath; // .../VokunModManager/appConfig.txt ; will store it in .txt for now

    public async Task UpdateConfig(string key, string value)
    {
        ConfigStates[key] = value;
        await WriteConfig();
    }
    
    private async Task WriteConfig()
    {
        List<string> newLines = new(); // will take it as a single string
        
        foreach (var key in ConfigStates.Keys)
        {
            newLines.Add($"{key}={ConfigStates[key]}"); // '=' is the only separator
        }
        
        await File.WriteAllLinesAsync(configPath, newLines);
    }
    
    private async Task InitConfig()
    {
        if (!File.Exists(configPath)) File.Create(configPath);

        var lines = await File.ReadAllLinesAsync(configPath);
        ConfigStates = new Dictionary<string, string>();

        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line) || !line.Contains('='))
                continue;

            var parts = line.Split('=', 2); // 2 parts only
            var key = parts[0].Trim();
            var value = parts[1].Trim();

            ConfigStates[key] = value;
        }
        
        // manage the other stuff here....
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
}