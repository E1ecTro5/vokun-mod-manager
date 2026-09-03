using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VokunModManager.Interfaces;
using VokunModManager.Models;

namespace VokunModManager.ViewModels;

// A LOT of things should be refactored here, I hope you won't forget

public partial class MainWindowViewModel : ViewModelBase
{
    [ObservableProperty] private ObservableCollection<Mod>? _modList;
    [ObservableProperty] private ObservableCollection<Mod>? _foundMods;
    [ObservableProperty] private ObservableCollection<ArchiveNode>? _archiveItems; // items shown in specific border
    
    // they all need to be displayed just to make it easier for me
    [ObservableProperty] private string? _gameFolderPath;     // Steam game folder
    [ObservableProperty] private string? _pluginFilePath;     // plugins.txt file
    [ObservableProperty] private string? _skyrimPrefsFilePath;

    [ObservableProperty] private bool _isPlayAvailable;
    [ObservableProperty] private bool _isLoadArchiveAvailable;
    [ObservableProperty] private bool _isModInstalling;
    
    // tools
    // will be deleted soon, probably?
    [ObservableProperty] private string? _pathToFnisTool;
    [ObservableProperty] private bool _isFnisAvailable;
    [ObservableProperty] private string? _pathToBodySlide;
    [ObservableProperty] private bool _isBodySlideAvailable;
    [ObservableProperty] private string? _pathToOutfitStudio;
    [ObservableProperty] private bool _isOutfitStudioAvailable;
    [ObservableProperty] private string? _pathToNemesisTool;
    [ObservableProperty] private bool _isNemesisAvailable;
    [ObservableProperty] private string? _pathToXEditTool;
    [ObservableProperty] private bool _isXEditAvailable;
    [ObservableProperty] private string? _pathToXEditAutoCleanTool;
    [ObservableProperty] private bool _isXEditAutoCleanAvailable;
    [ObservableProperty] private string? _pathToPandoraTool;
    [ObservableProperty] private bool _isPandoraAvailable;
    [ObservableProperty] private string? _pathToBethIniTool;
    [ObservableProperty] private bool _isBethIniAvailable;

    private readonly IFileManager _fileManager;
    private readonly IAutoDetector _autoDetector;
    private readonly IModInstaller _modInstaller;
    private readonly IModListManager _modListManager;
    
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
    
    // tools' commands
    public ICommand OpenFnisCommand { get; }
    public ICommand OpenOutfitStudioCommand { get; }
    public ICommand OpenBodySlideCommand { get; }
    public ICommand OpenNemesisCommand { get; }
    public ICommand OpenXEditCommand { get; }
    public ICommand OpenXEditAutoCleanCommand { get; }
    public ICommand OpenPandoraCommand { get; }
    public ICommand OpenBethIniCommand { get; }

    public MainWindowViewModel(
        IFileManager fileManager,
        IAutoDetector autoDetector,
        ILoggerService loggerService,
        IModInstaller modInstaller,
        IModListManager modListManager)
    {
        _fileManager = fileManager;
        _autoDetector = autoDetector;
        Logger = loggerService;
        Logger.Log("Logger initialized.");
        _modInstaller = modInstaller;
        _modListManager = modListManager;
        
        ModList = new ObservableCollection<Mod>();

        SelectDirectoryCommand = new AsyncRelayCommand(SetGamePath);
        SelectFileCommand = new AsyncRelayCommand(SetModListPath);
        ReInitTextBlocksCommand = new AsyncRelayCommand(ReInitValues);  // possible rename because of refactor ; remind me later if needed
        UpdateModListCommand = new AsyncRelayCommand(UpdateModList);
        
        OpenDataFolderCommand = new AsyncRelayCommand(OpenDataFolder);
        OpenPluginFileCommand = new AsyncRelayCommand(OpenPluginFile);
        OpenGameConfigCommand = new AsyncRelayCommand(OpenGameConfig);
        
        //tools
        OpenFnisCommand = new AsyncRelayCommand(OpenFnis);
        OpenOutfitStudioCommand = new AsyncRelayCommand(OpenOutfitStudio);
        OpenBodySlideCommand = new AsyncRelayCommand(OpenBodySlide);
        OpenNemesisCommand = new AsyncRelayCommand(OpenNemesis);
        OpenXEditCommand = new AsyncRelayCommand(OpenXEdit);
        OpenXEditAutoCleanCommand = new AsyncRelayCommand(OpenXEditAutoClean);
        OpenPandoraCommand = new AsyncRelayCommand(OpenPandora);
        OpenBethIniCommand = new AsyncRelayCommand(OpenBethIni);

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
        // may be null/default if Skyrim not installed, or you're launching for the first time.
        // please, make sure they're initialized before using
        GameFolderPath = AppConfig.Instance.GameFolderPath;
        PluginFilePath = AppConfig.Instance.PluginFilePath;
        SkyrimPrefsFilePath = AppConfig.Instance.SkyrimPrefsFilePath;

        PathToFnisTool = _autoDetector.TryGetFnisExecutable();
        PathToBodySlide = _autoDetector.TryGetBodySlideExecutable();
        PathToOutfitStudio = _autoDetector.TryGetOutfitStudioExecutable();
        PathToNemesisTool = _autoDetector.TryGetNemesisExecutable();
        PathToXEditTool = _autoDetector.TryGetSseeditExecutable();
        PathToXEditAutoCleanTool = _autoDetector.TryGetSseeditAutoCleanExecutable();
        PathToPandoraTool = _autoDetector.TryGetPandoraExecutable();
        PathToBethIniTool = _autoDetector.TryGetBethIniExecutable();
        
        IsFnisAvailable = CheckForExecutable(PathToFnisTool);
        IsBodySlideAvailable = CheckForExecutable(PathToBodySlide);
        IsOutfitStudioAvailable = CheckForExecutable(PathToOutfitStudio);
        IsNemesisAvailable = CheckForExecutable(PathToNemesisTool);
        IsXEditAvailable = CheckForExecutable(PathToXEditTool);
        IsXEditAutoCleanAvailable = CheckForExecutable(PathToXEditAutoCleanTool);
        IsPandoraAvailable = CheckForExecutable(PathToPandoraTool);
        IsBethIniAvailable = CheckForExecutable(PathToBethIniTool);
        
        IsPlayAvailable = true; // no need for checking the launcher ID since I'm gonna delete it anyway
        
        IsLoadArchiveAvailable = true;
    }

    private bool CheckForExecutable(string? path)
    {
        if (string.IsNullOrEmpty(path)) return false;
        if (!File.Exists(path)) return false;
        return true;
    }

    private async Task OpenFnis()
    {
        string relativeFnisPath = Path.Combine("Data", "tools", "GenerateFNIS_for_Users", "GenerateFNISForUsers.exe");
        
        await LaunchToolInProtonAsync(relativeFnisPath, () =>
        {
            // fix the 2001 error in FNIS
            string fnisDirPath = Path.GetDirectoryName(PathToFnisTool)!;
            string fnisIniPath = Path.Combine(fnisDirPath, "FNIS.ini");
            if (!File.Exists(fnisIniPath))
            {
                File.WriteAllText(fnisIniPath, "[Language]\nLanguage=ENGLISH\n\n[Path]\nData=Data\n");
            }
        });
    }

    private async Task OpenBodySlide()
    {
        string relativeBodySlidePath = Path.Combine("Data", "CalienteTools", "BodySlide", "BodySlide.exe");
        await LaunchToolInProtonAsync(relativeBodySlidePath);
    }
    
    private async Task OpenOutfitStudio()
    {
        string relativeStudioPath = Path.Combine("Data", "CalienteTools", "BodySlide", "OutfitStudio.exe");
        await LaunchToolInProtonAsync(relativeStudioPath);
    }

    private async Task OpenNemesis()
    {
        string relativeStudioPath = Path.Combine("Data", "Nemesis_Engine", "Nemesis Unlimited Behavior Engine.exe");
        await LaunchToolInProtonAsync(relativeStudioPath);
    }
    
    private async Task OpenXEdit()
    {
        string? relativeStudioPath = PathToXEditTool;
        if(string.IsNullOrEmpty(relativeStudioPath)) return;
        
        string[] dirs = relativeStudioPath.Split(Path.DirectorySeparatorChar);
        string relativeFromData = Path.Combine(
            dirs.SkipWhile(d => !d.Equals("Data", StringComparison.OrdinalIgnoreCase)).ToArray());
        
        await LaunchToolInProtonAsync(relativeFromData);
    }
    
    private async Task OpenXEditAutoClean()
    {
        string? relativeStudioPath = PathToXEditAutoCleanTool;
        if(string.IsNullOrEmpty(relativeStudioPath)) return;
        
        string[] dirs = relativeStudioPath.Split(Path.DirectorySeparatorChar);
        string relativeFromData = Path.Combine(
            dirs.SkipWhile(d => !d.Equals("Data", StringComparison.OrdinalIgnoreCase)).ToArray());
        
        await LaunchToolInProtonAsync(relativeFromData);
    }

    private async Task OpenPandora()
    {
        string relativeStudioPath = Path.Combine("Data", "Pandora Behaviour Engine+.exe");
        await LaunchToolInProtonAsync(relativeStudioPath);
    }

    private async Task OpenBethIni()
    {
        string? relativeStudioPath = PathToBethIniTool;
        if(string.IsNullOrEmpty(relativeStudioPath)) return;
        
        string[] dirs = relativeStudioPath.Split(Path.DirectorySeparatorChar);
        string relativeFromData = Path.Combine(
            dirs.SkipWhile(d => !d.Equals("Data", StringComparison.OrdinalIgnoreCase)).ToArray());
        
        await LaunchToolInProtonAsync(relativeFromData);
    }
    
    private async Task LaunchToolInProtonAsync(string pathToTool, Action? preLaunchSetup = null)
    {
        if(string.IsNullOrEmpty(GameFolderPath)) return; // just in case
        
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            string launcherPath = Path.Combine(GameFolderPath, "SkyrimSELauncher.exe");
            string backupLauncherPath = Path.Combine(GameFolderPath, "SkyrimSELauncher_backup.exe");
            string helperSource = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Utils", "ToolLauncher.exe");
            string configPath = Path.Combine(GameFolderPath, "vokun_tool_config.txt"); // out specific config file

            try
            {
                if (!File.Exists(helperSource))
                {
                    Logger.Log($"Tool executable not found at: {helperSource}", LogLevel.Error);
                    return;
                }

                // backup the launcher if not symlink
                if (File.Exists(launcherPath) && !File.Exists(backupLauncherPath))
                {
                    var fileInfo = new FileInfo(launcherPath);
                    if ((fileInfo.Attributes & FileAttributes.ReparsePoint) == 0)
                    {
                        File.Move(launcherPath, backupLauncherPath);
                    }
                }

                // for specific reasons (like fix error 2001 in FNIS)
                preLaunchSetup?.Invoke();

                // write path to our tool, so it'll launch the right one
                await File.WriteAllTextAsync(configPath, pathToTool);

                // replace the launcher
                if (File.Exists(launcherPath)) File.Delete(launcherPath);
                File.Copy(helperSource, launcherPath, overwrite: true);

                // run
                Process.Start(new ProcessStartInfo
                {
                    FileName = "steam://rungameid/489830",
                    UseShellExecute = true,
                });

                await Task.Delay(3000);
            }
            catch (Exception ex)
            {
                Logger.Log($"Error launching tool in Proton: {ex.Message}", LogLevel.Error);
            }
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = pathToTool,
                WorkingDirectory = GameFolderPath,
                UseShellExecute = true
            };

            Process.Start(startInfo);
        }
    }   
    
    private async Task ReInitValues()
    {
        AppConfig.Instance.CheckConfigStatus();
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
        await AppConfig.Instance.UpdateConfig(AppConfig.ConfigType.GameFolderPath, this.GameFolderPath);
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
        await AppConfig.Instance.UpdateConfig(AppConfig.ConfigType.PluginFilePath, PluginFilePath);
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
}