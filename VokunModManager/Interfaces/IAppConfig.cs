namespace VokunModManager.Interfaces;

public interface IAppConfig
{
    public string? GameFolderPath { get; }
    public string? CompatdataFolderPath { get; }
    public string? PluginFilePath { get; }
    public string? SkyrimPrefsFilePath { get; }

    public Task UpdateConfig(AppConfig.ConfigType key, string value);
    public Task InitConfig();
    public void CheckConfigStatus();
}