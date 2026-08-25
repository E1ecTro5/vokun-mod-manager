using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VokunModManager.Misc;
using VokunModManager.Models;

namespace VokunModManager.ViewModels;

// A LOT of things should be refactored here, I hope you won't forget

public partial class MainWindowViewModel : ViewModelBase
{
    [ObservableProperty] private ObservableCollection<Mod> _modList;
    [ObservableProperty] private ObservableCollection<Mod> _foundMods;
    [ObservableProperty] private ObservableCollection<ArchiveNode> _archiveItems; // items shown in specific border
    
    // they all need to be displayed just to make it easier for me
    [ObservableProperty] private string _gameFolderPath;     // Steam game folder
    [ObservableProperty] private string _pluginFilePath;     // plugins.txt file
    [ObservableProperty] private string _vdfFilePath;        // shortcuts.vdf file from Steam/userinfo/../config/..
    [ObservableProperty] private ulong _modGameId;           // need for launching the skse64_loader.exe
    [ObservableProperty] private ulong _compatdataFolder;
    [ObservableProperty] private ulong _launcherId;
    [ObservableProperty] private string _skyrimPrefsFilePath;
    
    [ObservableProperty] private string _archivePath;        // path of a selected archive

    [ObservableProperty] private bool _isPlayAvailable;
    [ObservableProperty] private bool _isLoadArchiveAvailable;
    [ObservableProperty] private bool _isModInstalling;

    private readonly FileManager _fileManager = new FileManager();
    
    public ICommand SelectDirectoryCommand { get; }
    public ICommand SelectFileCommand { get; }
    public ICommand ReInitTextBlocksCommand { get; }
    public ICommand UpdateModListCommand { get; }
    public ICommand PlayClickCommand { get; }
    public ICommand SelectVdfCommand { get; }
    public ICommand SelectLoaderCompatdataCommand { get; }
    public ICommand SaveModListCommand { get; }
    public ICommand InstallModCommand { get; }
    
    public ICommand OpenDataFolderCommand { get; }
    public ICommand OpenPluginFileCommand { get; }
    public ICommand OpenGameConfigCommand { get; }

    public ILoggerService Logger { get; }
    
    public MainWindowViewModel()
    {
        ModList = new ObservableCollection<Mod>();

        SelectDirectoryCommand = new AsyncRelayCommand(SetGamePath);
        SelectFileCommand = new AsyncRelayCommand(SetModListPath);
        ReInitTextBlocksCommand = new AsyncRelayCommand(LateInit);  // possible rename because of refactor ; remind me later if needed
        UpdateModListCommand = new AsyncRelayCommand(UpdateModList);
        SelectVdfCommand = new AsyncRelayCommand(SelectVdf);
        SelectLoaderCompatdataCommand = new AsyncRelayCommand(SelectLoaderCompatdata);
        
        OpenDataFolderCommand = new AsyncRelayCommand(OpenDataFolder);
        OpenPluginFileCommand = new AsyncRelayCommand(OpenPluginFile);
        OpenGameConfigCommand = new AsyncRelayCommand(OpenGameConfig);

        PlayClickCommand = new AsyncRelayCommand(StartGame);
        SaveModListCommand = new AsyncRelayCommand(SaveModList);

        InstallModCommand = new AsyncRelayCommand(InstallMod);

        Logger = new UILoggerService();
        Logger.Log("Logger initialized.");
    }

    private async Task StartGame()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            string sksePath = Path.Combine(GameFolderPath, "skse64_loader.exe");
            string launcherPath = Path.Combine(GameFolderPath, "SkyrimSELauncher.exe");
            string backupPath = Path.Combine(GameFolderPath, "SkyrimSELauncher_backup.exe");

            // check if skse installed
            if (File.Exists(sksePath))
            {
                try
                {
                    // hide the original launcher to '_backup'
                    if (File.Exists(launcherPath) && !File.Exists(backupPath))
                        File.Move(launcherPath, backupPath);

                    // copy skse64_loader.exe and rename to SkyrimSELauncher.exe
                    File.Copy(sksePath, launcherPath, overwrite: true);
                }
                catch (Exception ex)
                {
                    Logger.Log($"Error while changing files: {ex.Message}", LogLevel.Error);
                }
            }
            
            // launch the game (should start the skse loader)
            Process.Start(new ProcessStartInfo
            {
                FileName = "steam://rungameid/489830",
                UseShellExecute = true,
                CreateNoWindow = true
            });
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            string pathToLauncher = Path.Combine(GameFolderPath, "skse64_loader.exe");
            var startInfo = new ProcessStartInfo
            {
                FileName = pathToLauncher,
                WorkingDirectory = GameFolderPath,
                UseShellExecute = true
            };

            Process.Start(startInfo);
        }
    }
    
    private async Task LateInit()
    {
        // may be null/default if Skyrim not installed, or you're launching for the first time.
        // please, make sure they're initialized before using
        GameFolderPath = AppConfig.Instance.GameFolderPath;
        PluginFilePath = AppConfig.Instance.PluginFilePath;
        ModGameId = AppConfig.Instance.LauncherId;
        VdfFilePath = AppConfig.Instance.VdfConfigPath;

        CompatdataFolder = AppConfig.Instance.CompatdataFolderId;
        LauncherId = AppConfig.Instance.LauncherId;
        SkyrimPrefsFilePath = AppConfig.Instance.SkyrimPrefsFilePath;
        
        ArchivePath = "Not selected"; // default
        
        IsPlayAvailable = true; // no need for checking the launcher ID since I'm gonna delete it anyway
        
        IsLoadArchiveAvailable = true;
    }

    private async Task ReInitValues()
    {
        await AppConfig.Instance.CheckConfigStatus();
    }

    private async Task UpdateModList()
    {
        var modListM = new ModListManager();
        var updated = await modListM.UpdateModList();
        ModList = updated;
        Logger.Log("Mod list updated.");
    }
    
    private async Task SaveModList()
    {
        var modListM = new ModListManager();
        // save current state
        await modListM.SaveCurrentModListState(ModList);
        await UpdateModList();
    }

    private async Task SelectVdf()
    {
        var filePath = await _fileManager.SelectFile();
        
        if (string.IsNullOrEmpty(filePath))
        {
            await MsgBoxManager.ShowWarning("Vdf file path not selected!");
            return;
        }

        VdfFilePath = filePath;
        await AppConfig.Instance.UpdateConfig(AppConfig.ConfigType.VdfConfigPath, filePath);
    }

    private async Task SelectLoaderCompatdata()
    {
        var compatdataDir = await _fileManager.SelectDirectory();
        
        if (string.IsNullOrEmpty(compatdataDir))
        {
            await MsgBoxManager.ShowWarning("Compatdata folder path not selected!");
            return;
        }

        await AppConfig.Instance.UpdateConfig(AppConfig.ConfigType.CompatdataFolder, compatdataDir);
    }

    private async Task SetGamePath()
    {
        var filePath = await _fileManager.SelectDirectory();

        if (string.IsNullOrEmpty(filePath))
        {
            await MsgBoxManager.ShowWarning("Game path not selected!");
            return;
        }
        
        GameFolderPath = filePath;
        await AppConfig.Instance.UpdateConfig(AppConfig.ConfigType.GameFolderPath, this.GameFolderPath);
    }

    private async Task SetModListPath()
    {
        var filePath = await _fileManager.SelectFile();

        if (string.IsNullOrEmpty(filePath))
        {
            await MsgBoxManager.ShowWarning("Mod file path not selected!");
            return;
        }

        PluginFilePath = filePath;
        await AppConfig.Instance.UpdateConfig(AppConfig.ConfigType.PluginFilePath, PluginFilePath);
    }

    private async Task OpenDataFolder()
    {
        if (string.IsNullOrEmpty(GameFolderPath))
        {
            await MsgBoxManager.ShowWarning("Game folder path not selected!");
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
        await OpenFileDirectory(AppConfig.Instance.SkyrimPrefsFilePath);
    }

    private async Task OpenFileDirectory(string path)
    {
        if(string.IsNullOrEmpty(path)) return;

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            var psi = new ProcessStartInfo
            {
                FileName = "xdg-open", // this should work only on Linux (Wayland?)
                Arguments = $"\"{path}\"",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            Process.Start(psi);
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            Process.Start("explorer.exe", path);
        }
        
    }

    private async Task InstallMod()
    {
        var filePath = await _fileManager.SelectFile();
        if (string.IsNullOrEmpty(filePath))
        {
            await MsgBoxManager.ShowWarning("Mod archive not selected!");
            return;
        }

        IsModInstalling = true;
        Logger.Log($"Selected file: {filePath}");
        var fomod = new FomodManager(filePath, Logger);
        IsPlayAvailable = false;
        await fomod.InstallMod();
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