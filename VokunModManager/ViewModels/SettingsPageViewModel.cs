using System.Diagnostics;
using System.Runtime.InteropServices;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VokunModManager.Interfaces;
using VokunModManager.Models;

namespace VokunModManager.ViewModels;

public partial class SettingsPageViewModel : ViewModelBase
{
    [ObservableProperty] private string? _gameFolderPath;     // folder inside "steamapps/common"
    [ObservableProperty] private string? _compatdataFolderPath;
    [ObservableProperty] private string? _pluginFilePath;     // plugins.txt file
    [ObservableProperty] private string? _skyrimPrefsFilePath;
    
    // RENAME EVERYTHING??
    
    [ObservableProperty] private bool _isGameFolderPathFound;
    [ObservableProperty] private bool _isCompatdataFolderPathFound;
    [ObservableProperty] private bool _isPluginsFilePathFound;
    [ObservableProperty] private bool _isGameConfigFound;
    
    private readonly IAppConfig _appConfig;
    private readonly ILoggerService _logger;
    private readonly IFileManager _fileManager;

    public SettingsPageViewModel(
        IAppConfig appConfig,
        ILoggerService loggerService,
        IFileManager fileManager)
    {
        _appConfig = appConfig;
        _logger = loggerService;
        _fileManager = fileManager;
    }

    public async Task LateInit()
    {
        GameFolderPath = _appConfig.GameFolderPath;
        CompatdataFolderPath = _appConfig.CompatdataFolderPath;
        PluginFilePath = _appConfig.PluginFilePath;
        SkyrimPrefsFilePath = _appConfig.SkyrimPrefsFilePath;

        IsGameFolderPathFound = GameFolderPath != null;
        IsCompatdataFolderPathFound = CompatdataFolderPath != null;
        IsPluginsFilePathFound = PluginFilePath != null;
        IsGameConfigFound = SkyrimPrefsFilePath != null;
    }
    
    [RelayCommand]
    private async Task SetGameFolderPath()
    {
        var filePath = await _fileManager.SelectDirectoryAsync();

        if (string.IsNullOrEmpty(filePath))
        {
            _logger.Log("Game path has not been selected!", LogLevel.Error);
            return;
        }

        await _appConfig.UpdateConfig(AppConfig.ConfigType.GameFolderPath, filePath);
    }

    [RelayCommand]
    private async Task SetCompatdataFolder()
    {
        var directoryPath = await _fileManager.SelectDirectoryAsync();

        if (string.IsNullOrEmpty(directoryPath))
        {
            _logger.Log("Game path has not been selected!", LogLevel.Error);
            return;
        }

        await _appConfig.UpdateConfig(AppConfig.ConfigType.CompatdataFolderPath, directoryPath);
    }
    
    [RelayCommand]
    private async Task SetPluginsFilePath()
    {
        var filePath = await _fileManager.SelectFileAsync();

        if (string.IsNullOrEmpty(filePath))
        {
            _logger.Log("Mod file path has not been selected!", LogLevel.Error);
            return;
        }

        await _appConfig.UpdateConfig(AppConfig.ConfigType.PluginFilePath, filePath);
    }

    [RelayCommand]
    private async Task SetGameConfigFile()
    {
        var filePath = await _fileManager.SelectFileAsync();

        if (string.IsNullOrEmpty(filePath))
        {
            _logger.Log("Prefs.ini file has not been selected!", LogLevel.Error);
            return;
        }

        await _appConfig.UpdateConfig(AppConfig.ConfigType.PluginFilePath, filePath);
    }
    
    /*
    private async Task ReInitValues()
    {
        _appConfig.CheckConfigStatus();
    }
    */

    [RelayCommand]
    private async Task OpenDataFolder()
    {
        var gameFolderPath = _appConfig.GameFolderPath;
        if (string.IsNullOrEmpty(gameFolderPath))
        {
            _logger.Log("Game folder path not selected!", LogLevel.Error);
            return;
        }
        await OpenFileDirectory(gameFolderPath);
    }

    [RelayCommand]
    private async Task OpenCompatdataFolder()
    {
        var compatdataFolderPath = _appConfig.CompatdataFolderPath;
        if (string.IsNullOrEmpty(compatdataFolderPath))
        {
            _logger.Log("Compatdata folder path not selected!", LogLevel.Error);
            return;
        }
        await OpenFileDirectory(compatdataFolderPath);
    }
    
    [RelayCommand]
    private async Task OpenPluginFile()
    {
        var pluginFilePath = _appConfig.PluginFilePath;
        await OpenFileDirectory(pluginFilePath);
    }

    [RelayCommand]
    private async Task OpenGameConfigFile()
    {
        var gameConfigPath = _appConfig.SkyrimPrefsFilePath;
        await OpenFileDirectory(gameConfigPath);
    }

    private async Task OpenFileDirectory(string? path)
    {
        if (string.IsNullOrWhiteSpace(path)) return;
        if (!File.Exists(path) && !Directory.Exists(path)) return;

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "xdg-open",
                Arguments = $"\"{path}\"",
                UseShellExecute = true, // important on Linux
                CreateNoWindow = true
            });
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = path,
                UseShellExecute = true
            });
        }
    }
}