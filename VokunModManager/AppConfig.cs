using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
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
        CompatdataFolder
    }
    
    public string BaseDirectory { get; private set; } // ../VokunModManager directory
    private readonly string _appConfigPath; // .../VokunModManager/appConfig.txt ; will store it in .txt for now

    public string GameFolderPath { get; private set; } // Skyrim Steam folder
    public string PluginFilePath { get; private set; } // path for Plugins.txt
    public string VdfConfigPath { get; private set; } // shortcuts.vdf file path ; this is used to get non-steam game ID
    public ulong CompatdataFolderId { get; private set; } // ID of skse64 compatdata folder
    public ulong GameId { get; private set; } // skse64_launcher.exe ID, needed to launch from steam

    public async Task UpdateConfig(ConfigType key, string value)
    {
        switch (key)
        {
            case ConfigType.GameFolderPath:
                GameFolderPath = value;
                break;
            case ConfigType.PluginFilePath:
                PluginFilePath = value;
                break;
            case ConfigType.VdfConfigPath:
                VdfConfigPath = value;
                break;
            case ConfigType.CompatdataFolder:
                CompatdataFolderId = await TryGetValueFromDirectory(value);
                GameId = await GetGameId();
                break;
            default: return; // return if not match
        }
        
        await ReWriteConfig(); // don't forget to update
    }
    
    public async Task InitConfig()
    {
        if (!File.Exists(_appConfigPath)) await using (File.Create(_appConfigPath)) { } // DON'T FORGET TO CLOSE THE STREAM! USE using

        var lines = await File.ReadAllLinesAsync(_appConfigPath);

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
                
                // since you WRITE FIRST and READ ONLY THEN, we don't expect excep there
                case "compatdataFolderId": CompatdataFolderId = Convert.ToUInt64(value); break;
                case "gameId": GameId = Convert.ToUInt64(value); break;
                default: continue; // skip if not match
            }
        }

        await CheckConfigStatus(); // just to be sure
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

        // we expect to get an ID -> ../{ID}/pfx/... improvements 
        //modGameSteamId = Convert.ToUInt64(dir.Parent.Name);
        // bad practice?
        string result = dir.Parent.Name;
        return Convert.ToUInt64(result);
    }

    private async Task<ulong> GetGameId()
    {
        string path = Path.Combine(GameFolderPath, "skse64_loader.exe");
        ulong result = 0;
        
        try
        {
            result = await GameIdFinder.GetLongId(path);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }

        return result;
    }

    private async Task<ulong> TryGetValueFromDirectory(string path)
    {
        if(ulong.TryParse(path, out ulong result)) return result;
        return 0; // always check for 0 like you check for null or empty
    }

    private async Task CheckConfigStatus()
    {
        var fileM = new FileManager();

        if (string.IsNullOrEmpty(GameFolderPath)) await fileM.TryGetGameFolder();
        if (string.IsNullOrEmpty(PluginFilePath)) await fileM.TryGetPluginConfig();
        if (string.IsNullOrEmpty(VdfConfigPath)) await fileM.TryGetVdfConfig();
    }
}