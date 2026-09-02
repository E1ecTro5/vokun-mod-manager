using VokunModManager.Interfaces;
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
    }

    public enum ConfigType
    {
        GameFolderPath,
        PluginFilePath,
        VdfConfigPath,
        CompatdataFolder,
        SkyrimPrefsFilePath
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
    /// Path to "SkyrimPrefs" file. Basically, the in-game settings config.
    /// </summary>
    public string? SkyrimPrefsFilePath { get; private set; }
    
    // ======== ===== ========

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
            case ConfigType.SkyrimPrefsFilePath:
                SkyrimPrefsFilePath = value;
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
                // since you WRITE FIRST and READ LATER, we don't expect exception there ; may be just empty
                case "skyrimPrefsFilePath": SkyrimPrefsFilePath = value; break;
                default:
                    await MsgBoxManager.ShowWarning($"Couldn't identify key: {key} while initializing the config.");
                    continue; // skip if not match
            }
        }
        
        CheckConfigStatus(); // just to be sure
    }
    
    private async Task ReWriteConfig()
    {
        await using (StreamWriter sw = new StreamWriter(_appConfigPath))
        {
            // GAME PATH GOES FIRST ; MODFILE (Plugins.txt) GOES SECOND and so on... it's hardcoded.
            await sw.WriteLineAsync($"gameFolderPath={GameFolderPath}");
            await sw.WriteLineAsync($"pluginFilePath={PluginFilePath}");
            await sw.WriteLineAsync($"skyrimPrefsFilePath={SkyrimPrefsFilePath}");
        }
    }
    
    public void CheckConfigStatus()
    {
        IAutoDetector detector = new AutoDetector(); 
        
        if (string.IsNullOrEmpty(GameFolderPath)) GameFolderPath = detector.TryGetGameFolder();
        if (string.IsNullOrEmpty(PluginFilePath)) PluginFilePath = detector.TryGetPluginConfig();
        if (string.IsNullOrEmpty(SkyrimPrefsFilePath)) SkyrimPrefsFilePath = detector.TryGetPrefsFile();
    }
}