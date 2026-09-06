using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VokunModManager.Interfaces;
using VokunModManager.Models;

namespace VokunModManager.ViewModels;

public partial class MainPageViewModel : ViewModelBase
{
    [ObservableProperty] private ObservableCollection<Mod>? _modList;
    [ObservableProperty] private ObservableCollection<Mod>? _foundMods;
    [ObservableProperty] private ObservableCollection<ArchiveNode>? _archiveItems; // items shown in specific border
    
    // they all need to be displayed just to make it easier for me
    [ObservableProperty] private string? _gameFolderPath;     // Steam game folder
    [ObservableProperty] private string? _compatdataFolderPath;
    [ObservableProperty] private string? _pluginFilePath;     // plugins.txt file
    [ObservableProperty] private string? _skyrimPrefsFilePath;

    [ObservableProperty] private bool _isPlayAvailable;
    [ObservableProperty] private bool _isLoadArchiveAvailable;
    [ObservableProperty] private bool _isModInstalling;

    private readonly IAppConfig _appConfig;
    private readonly IFileManager _fileManager;
    private readonly IAutoDetector _autoDetector;
    private readonly IModInstaller _modInstaller;
    private readonly IModListManager _modListManager;
    private readonly IGameStateResetter _gameStateResetter;
    
    public ILoggerService Logger { get; }
    
    public ICommand SelectDirectoryCommand { get; }
    public ICommand SelectFileCommand { get; }
    public ICommand ReInitTextBlocksCommand { get; }
    public ICommand UpdateModListCommand { get; }
    public ICommand PlayClickCommand { get; }
    public ICommand SaveModListCommand { get; }
    public ICommand InstallModCommand { get; }
    
    public ICommand OpenDataFolderCommand { get; }
    public ICommand OpenPluginFileCommand { get; }
    public ICommand OpenGameConfigCommand { get; }
    
    // resetter
    public ICommand SaveCurrentGameStateCommand { get; }
    public ICommand ResetGameStateCommand { get; }

    public MainPageViewModel(
        IAppConfig appConfig,
        IFileManager fileManager,
        IAutoDetector autoDetector,
        ILoggerService loggerService,
        IModInstaller modInstaller,
        IModListManager modListManager,
        IGameStateResetter gameStateResetter)
    {
        _appConfig = appConfig;
        _fileManager = fileManager;
        _autoDetector = autoDetector;
        Logger = loggerService;
        Logger.Log("Logger initialized.");
        _modInstaller = modInstaller;
        _modListManager = modListManager;
        _gameStateResetter = gameStateResetter;
        
        ModList = new ObservableCollection<Mod>();

        SelectDirectoryCommand = new AsyncRelayCommand(SetGamePath);
        SelectFileCommand = new AsyncRelayCommand(SetModListPath);
        ReInitTextBlocksCommand = new AsyncRelayCommand(ReInitValues);  // possible rename because of refactor ; remind me later if needed
        UpdateModListCommand = new AsyncRelayCommand(UpdateModList);
        
        OpenDataFolderCommand = new AsyncRelayCommand(OpenDataFolder);
        OpenPluginFileCommand = new AsyncRelayCommand(OpenPluginFile);
        OpenGameConfigCommand = new AsyncRelayCommand(OpenGameConfig);

        SaveCurrentGameStateCommand = new AsyncRelayCommand(SaveGameCurrentState);
        ResetGameStateCommand = new AsyncRelayCommand(ResetGameState);

        PlayClickCommand = new AsyncRelayCommand(StartGame);
        SaveModListCommand = new AsyncRelayCommand(SaveModList);

        InstallModCommand = new AsyncRelayCommand(InstallMod);
    }

    private async Task StartGame()
    {
        if(string.IsNullOrEmpty(GameFolderPath)) return;
        string skseLoaderPath = Path.Combine(GameFolderPath, "skse64_loader.exe");
        if(!File.Exists(skseLoaderPath)) return; // don't start if you don't have skse64.
        
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            string launcherPath = Path.Combine(GameFolderPath, "SkyrimSELauncher.exe");
            string backupPath = Path.Combine(GameFolderPath, "SkyrimSELauncher_backup.exe");
            
            try
            {
                // hide the original launcher to '_backup'
                if (File.Exists(launcherPath) && !File.Exists(backupPath))
                    File.Move(launcherPath, backupPath);

                // copy skse64_loader.exe and rename to SkyrimSELauncher.exe
                File.Copy(skseLoaderPath, launcherPath, overwrite: true);
            }
            catch (Exception ex)
            {
                Logger.Log($"Error while changing files: {ex.Message}", LogLevel.Error);
            }
            
            // launch the game (should start the skse loader)
            var startInfo = new ProcessStartInfo
            {
                FileName = "steam://rungameid/489830",
                UseShellExecute = true,
                CreateNoWindow = true
            };
            
            Process.Start(startInfo);
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = skseLoaderPath,
                WorkingDirectory = GameFolderPath,
                UseShellExecute = true
            };

            Process.Start(startInfo);
        }
    }
    
    private async Task LateInit()
    {
        await _appConfig.InitConfig();
        _appConfig.CheckConfigStatus();
        
        // may be null/default if Skyrim not installed, or you're launching for the first time.
        // please, make sure they're initialized before using
        GameFolderPath = _appConfig.GameFolderPath;
        CompatdataFolderPath = _appConfig.CompatdataFolderPath;
        PluginFilePath = _appConfig.PluginFilePath;
        SkyrimPrefsFilePath = _appConfig.SkyrimPrefsFilePath;
        
        IsPlayAvailable = true; // no need for checking the launcher ID since I'm gonna delete it anyway
        
        IsLoadArchiveAvailable = true;
    }
    
    private async Task ReInitValues()
    {
        _appConfig.CheckConfigStatus();
    }

    private async Task UpdateModList()
    {
        var updated = await _modListManager.UpdateModList();
        if (updated is null)
        {
            Logger.Log("Mod list is null.", LogLevel.Warning);
            return;
        }
        ModList = updated;
        Logger.Log("Mod list updated.");
    }
    
    private async Task SaveModList()
    {
        if (ModList is null)
        {
            Logger.Log("Mod list is null.", LogLevel.Error);
            return;
        }
        await _modListManager.SaveCurrentModListState(ModList); // has to be initialized at this time...
        await UpdateModList();
    }

    private async Task SetGamePath()
    {
        var filePath = await _fileManager.SelectDirectoryAsync();

        if (string.IsNullOrEmpty(filePath))
        {
            Logger.Log("Game path not selected!", LogLevel.Error);
            return;
        }
        
        GameFolderPath = filePath;
        await _appConfig.UpdateConfig(AppConfig.ConfigType.GameFolderPath, this.GameFolderPath);
    }

    private async Task SetModListPath()
    {
        var filePath = await _fileManager.SelectFileAsync();

        if (string.IsNullOrEmpty(filePath))
        {
            Logger.Log("Mod file path not selected!", LogLevel.Error);
            return;
        }

        PluginFilePath = filePath;
        await _appConfig.UpdateConfig(AppConfig.ConfigType.PluginFilePath, PluginFilePath);
    }

    private async Task OpenDataFolder()
    {
        if (string.IsNullOrEmpty(GameFolderPath))
        {
            Logger.Log("Game folder path not selected!", LogLevel.Error);
            return;
        }
        await OpenFileDirectory(GameFolderPath);
    }
    
    private async Task OpenPluginFile()
    {
        await OpenFileDirectory(PluginFilePath);
    }
    
    private async Task OpenGameConfig()
    {
        await OpenFileDirectory(SkyrimPrefsFilePath);
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

    private async Task InstallMod()
    {
        var filePath = await _fileManager.SelectFileAsync();
        if (string.IsNullOrEmpty(filePath))
        {
            Logger.Log("Mod archive not selected!", LogLevel.Error);
            return;
        }

        IsModInstalling = true;
        IsPlayAvailable = false;
        
        Logger.Log(string.Empty); // space for better visibility
        Logger.Log($"Selected file: {filePath}");
        await _modInstaller.InstallMod(filePath);
        Logger.Log(string.Empty); // space for better visibility
        
        IsPlayAvailable = true;
        IsModInstalling = false;
        
        await UpdateModList();
    }

    public async Task UpdateAll()
    {
        await LateInit();
        await UpdateModList(); // maybe you should include this in LateInit()
    }

    private async Task SaveGameCurrentState()
    {
        if (string.IsNullOrEmpty(GameFolderPath))
        {
            Logger.Log("Can't save manifest, because GameFolderPath is not initialized.", LogLevel.Warning);
            return;
        }

        string manifestPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "default_manifest.json");

        // Создаем слепок ТОЛЬКО если файл еще не существует
        if (File.Exists(manifestPath))
        {
            Logger.Log("Manifest already exists.");
            return;
        }

        try
        {
            Logger.Log("Generate clean game's manifest (default_manifest.json)...");

            await _gameStateResetter.CreateDefaultManifestAsync(GameFolderPath, CompatdataFolderPath, manifestPath);

            Logger.Log($"Manifest successfully created at: {manifestPath}");
        }
        catch (Exception ex)
        {
            Logger.Log($"Error while creating manifest: {ex.Message}", LogLevel.Error);
        }
    }

    private async Task ResetGameState()
    {
        if (string.IsNullOrEmpty(GameFolderPath))
        {
            Logger.Log("GameFolderPath is not initialized. Reset is unavailable.", LogLevel.Error);
            return;
        }

        try
        {
            IsPlayAvailable = false;
            var progress = new Progress<string>(message =>
            {
                Logger.Log(message);
            });
            Logger.Log("Resetting the game...");
            
            await _gameStateResetter.ResetToDefaultAsync(GameFolderPath, CompatdataFolderPath, progress);

            Logger.Log("Game state has been reset to default.");
            
            await UpdateModList();
        }
        catch (Exception ex)
        {
            Logger.Log($"Error while resetting the game: {ex.Message}", LogLevel.Error);
        }
        finally
        {
            IsPlayAvailable = true;
        }
    }
}