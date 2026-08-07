using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using VokunModManager.Misc;

namespace VokunModManager;

public sealed class AppConfig
{
    private static readonly Lazy<AppConfig> Lazy = new(() => new AppConfig());
    public static AppConfig Instance => Lazy.Value;

    private AppConfig()
    {
        var baseDirectory = AppContext.BaseDirectory; // path of the folder which contains the executable / binary
    
        _appConfigPath = Path.Combine(baseDirectory, "config.txt");
        TempFolder = Path.Combine(baseDirectory, "temp");
    }

    public enum ConfigType
    {
        GameFolderPath,
        PluginFilePath,
        VdfConfigPath,
        CompatdataFolder
    }

    // private readonly string _baseDirectory; // ../VokunModManager directory
    private readonly string _appConfigPath; // .../VokunModManager/appConfig.txt ; will store it in .txt for now

    // ======== PROPS ========
    
    /// <summary>
    /// Path to the "...steamapps/common/Skyrim Special Edition" folder.
    /// </summary>
    public string? GameFolderPath { get; private set; }
    /// <summary>
    /// Path for "...AppData/Local/Skyrim Special Edition/Plugins.txt" file.
    /// </summary>
    public string? PluginFilePath { get; private set; }
    /// <summary>
    /// Path to "...Steam/userdata/.../config/shortcuts.vdf" file. This is needed in order to get a non-steam game ID.
    /// </summary>
    public string? VdfConfigPath { get; private set; }
    /// <summary>
    /// [Linux ONLY] ID of "skse64" compatdata folder.
    /// </summary>
    public ulong CompatdataFolderId { get; private set; }
    /// <summary>
    /// ID of the "skse64_launcher.exe". This is the file, that will be launcher from steam.
    /// </summary>
    public ulong LauncherId { get; private set; }
    /// <summary>
    /// Path to "SkyrimPrefs" file. Basically, the in-game settings config.
    /// </summary>
    public string? SkyrimPrefsFilePath { get; private set; }
    /// <summary>
    /// Path to "temp" folder. Used to extract archive files directly in there, then move to Data folder of the game.
    /// It's a faster way of installing.
    /// </summary>
    public string TempFolder { get; }
    
    // ======== ===== ========

    public async Task UpdateConfig(ConfigType key, string value)
    {
        FileManager fm = new FileManager();
        
        switch (key)
        {
            case ConfigType.GameFolderPath:
                GameFolderPath = value;
                SkyrimPrefsFilePath = await fm.GetGameConfig();
                break;
            case ConfigType.PluginFilePath:
                PluginFilePath = value;
                break;
            case ConfigType.VdfConfigPath:
                VdfConfigPath = value;
                break;
            case ConfigType.CompatdataFolder:
                CompatdataFolderId = await fm.TryGetValueFromDirectory(value);
                LauncherId = await fm.GetGameId(GameFolderPath);
                break;
            default:
                await MsgBoxManager.ShowWarning($"Couldn't identify key: {key} while updating the config.");
                return; // return if not match
        }
        
        await ReWriteConfig(); // don't forget to update
    }
    
    public async Task InitConfig()
    {
        if (!File.Exists(_appConfigPath)) await File.WriteAllTextAsync(_appConfigPath, string.Empty);
        if (!Directory.Exists(TempFolder)) Directory.CreateDirectory(TempFolder);       // It just HAS to exist

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
                // since you WRITE FIRST and READ LATER, we don't expect exception there ; may be just empty
                case "compatdataFolderId": CompatdataFolderId = Convert.ToUInt64(value); break;
                case "gameId": LauncherId = Convert.ToUInt64(value); break;
                case "skyrimPrefsFilePath": SkyrimPrefsFilePath = value; break;
                default:
                    await MsgBoxManager.ShowWarning($"Couldn't identify key: {key} while initializing the config.");
                    continue; // skip if not match
            }
        }

        await CheckConfigStatus(); // just to be sure
    }
    
    private async Task ReWriteConfig()
    {
        await using (StreamWriter sw = new StreamWriter(_appConfigPath))
        {
            // GAME PATH GOES FIRST ; MODFILE (Plugins.txt) GOES SECOND and so on... it's hardcoded.
            await sw.WriteLineAsync($"gameFolderPath={GameFolderPath}");
            await sw.WriteLineAsync($"pluginFilePath={PluginFilePath}");
            await sw.WriteLineAsync($"vdfConfigPath={VdfConfigPath}");
            await sw.WriteLineAsync($"compatdataFolderId={CompatdataFolderId}");
            await sw.WriteLineAsync($"gameId={LauncherId}");
            await sw.WriteLineAsync($"skyrimPrefsFilePath={SkyrimPrefsFilePath}");
        }
    }
    
    public async Task CheckConfigStatus()
    {
        var fileM = new FileManager();

        if (string.IsNullOrEmpty(GameFolderPath)) await fileM.TryGetGameFolder();
        if (string.IsNullOrEmpty(PluginFilePath)) await fileM.TryGetPluginConfig();
        if (string.IsNullOrEmpty(VdfConfigPath)) await fileM.TryGetVdfConfig();
    }
}