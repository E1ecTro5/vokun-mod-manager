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
    
    public ICommand PlayClickCommand { get; }
    public ICommand SaveModListCommand { get; }
    public ICommand InstallModCommand { get; }

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

        PlayClickCommand = new AsyncRelayCommand(StartGame);
        SaveModListCommand = new AsyncRelayCommand(SaveModList);

        InstallModCommand = new AsyncRelayCommand(InstallMod);
    }

    private async Task StartGame()
    {
        var gameFolderPath = _appConfig.GameFolderPath;
        if(string.IsNullOrEmpty(gameFolderPath)) return;
        string skseLoaderPath = Path.Combine(gameFolderPath, "skse64_loader.exe");
        if(!File.Exists(skseLoaderPath)) return; // don't start if you don't have skse64.
        
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            string launcherPath = Path.Combine(gameFolderPath, "SkyrimSELauncher.exe");
            string backupPath = Path.Combine(gameFolderPath, "SkyrimSELauncher_backup.exe");
            
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
                WorkingDirectory = gameFolderPath,
                UseShellExecute = true
            };

            Process.Start(startInfo);
        }
    }
    
    private async Task LateInit()
    {
        await _appConfig.InitConfig();
        _appConfig.CheckConfigStatus();
        
        IsPlayAvailable = true; // no need for checking the launcher ID since I'm gonna delete it anyway
        
        IsLoadArchiveAvailable = true;
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
}