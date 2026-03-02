using System;
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
        TempFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "temp");
    }

    public enum ConfigType
    {
        GameFolderPath,
        PluginFilePath,
        VdfConfigPath,
        CompatdataFolder
    }
    
    public string BaseDirectory { get; } // ../VokunModManager directory
    private readonly string _appConfigPath; // .../VokunModManager/appConfig.txt ; will store it in .txt for now
    public string TempFolder { get; }

    public string GameFolderPath { get; private set; } // Skyrim Steam folder
    public string PluginFilePath { get; private set; } // path for Plugins.txt
    public string VdfConfigPath { get; private set; } // shortcuts.vdf file path ; this is used to get non-steam game ID
    public ulong CompatdataFolderId { get; private set; } // ID of skse64 compatdata folder
    public ulong GameId { get; private set; } // skse64_launcher.exe ID, needed to launch from steam
    public string SkyrimPrefsFilePath { get; private set; } // settings for the game 

    public async Task UpdateConfig(ConfigType key, string value)
    {
        switch (key)
        {
            case ConfigType.GameFolderPath:
                GameFolderPath = value;
                SkyrimPrefsFilePath = await GetGameConfig();
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
            default:
                await MsgBoxManager.ShowWarning($"Couldn't identify key: {key} while updating the config.");
                return; // return if not match
        }
        
        await ReWriteConfig(); // don't forget to update
    }
    
    public async Task InitConfig()
    {
        if (!File.Exists(_appConfigPath)) await using (File.Create(_appConfigPath)) { } // DON'T FORGET TO CLOSE THE STREAM! USE using
        if (!Directory.Exists(TempFolder)) Directory.CreateDirectory(TempFolder);

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
                // since you WRITE FIRST and READ ONLY THEN, we don't expect exception there
                case "compatdataFolderId": CompatdataFolderId = Convert.ToUInt64(value); break;
                case "gameId": GameId = Convert.ToUInt64(value); break;
                case "skyrimPrefsFilePath": SkyrimPrefsFilePath = value; break;
                default:
                    await MsgBoxManager.ShowWarning($"Couldn't identify key: {key} while initializing the config.");
                    continue; // skip if not match
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
            // since I build in one folder, root will be on the same level with a bunch of library files.
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
            await sw.WriteLineAsync($"skyrimPrefsFilePath={SkyrimPrefsFilePath}");
        }
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
            await MsgBoxManager.ShowWarning($"Couldn't identify GameID. Exception message: {ex.Message}");
        }

        return result;
    }

    private async Task<ulong> TryGetValueFromDirectory(string path)
    {
        if(ulong.TryParse(path, out ulong result)) return result;
        return 0; // always check for 0 like you check for null or empty
    }

    private async Task<string> GetGameConfig()
    {
        var loc = ".local/share/Steam/steamapps/compatdata/489830/pfx/drive_c/users/steamuser/My Documents/My Games/Skyrim Special Edition/SkyrimPrefs.ini";
        string possibleLoc = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), loc);
        if(File.Exists(possibleLoc)) return possibleLoc;
        return null;
    }

    private async Task CheckConfigStatus()
    {
        var fileM = new FileManager();

        if (string.IsNullOrEmpty(GameFolderPath)) await fileM.TryGetGameFolder();
        if (string.IsNullOrEmpty(PluginFilePath)) await fileM.TryGetPluginConfig();
        if (string.IsNullOrEmpty(VdfConfigPath)) await fileM.TryGetVdfConfig();
    }
}