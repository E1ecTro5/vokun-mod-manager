using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
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
    [ObservableProperty] private string _skyrimPrefsFilePath;

    [ObservableProperty] private bool _isPlayAvailable;
    [ObservableProperty] private bool _isLoadArchiveAvailable;
    [ObservableProperty] private bool _isModInstalling;
    
    // tools
    [ObservableProperty] private string _pathToFnisTool;
    [ObservableProperty] private bool _isFnisAvailable;
    [ObservableProperty] private string _pathToBodySlide;
    [ObservableProperty] private bool _isBodySlideAvailable;
    [ObservableProperty] private string _pathToOutfitStudio;
    [ObservableProperty] private bool _isOutfitStudioAvailable;
    [ObservableProperty] private string _pathToNemesisTool;
    [ObservableProperty] private bool _isNemesisAvailable;
    [ObservableProperty] private string _pathToSseeditTool;
    [ObservableProperty] private bool _isSseeditAvailable;
    [ObservableProperty] private string _pathToSseeditAutoCleanTool;
    [ObservableProperty] private bool _isSseeditAutoCleanAvailable;
    [ObservableProperty] private string _pathToPandoraTool;
    [ObservableProperty] private bool _isPandoraAvailable;

    private readonly FileManager _fileManager = new FileManager();
    private readonly AutoDetector _autoDetector = new AutoDetector();
    
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

    public ILoggerService Logger { get; }
    
    // tools' commands
    public ICommand OpenFnisCommand { get; }
    public ICommand DeleteFnisSymlinksCommand { get; }
    public ICommand OpenOutfitStudioCommand { get; }
    public ICommand OpenBodySlideCommand { get; }
    public ICommand OpenNemesisCommand { get; }
    public ICommand OpenSseeditCommand { get; }
    public ICommand OpenSseeditAutoCleanCommand { get; }
    public ICommand OpenPandoraCommand { get; }

    public MainWindowViewModel()
    {
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
        DeleteFnisSymlinksCommand = new AsyncRelayCommand(DeleteFnisSymlinks);
        OpenOutfitStudioCommand = new AsyncRelayCommand(OpenOutfitStudio);
        OpenBodySlideCommand = new AsyncRelayCommand(OpenBodySlide);
        OpenNemesisCommand = new AsyncRelayCommand(OpenNemesis);
        OpenSseeditCommand = new AsyncRelayCommand(OpenSseedit);
        OpenSseeditAutoCleanCommand = new AsyncRelayCommand(OpenSseeditAutoClean);
        OpenPandoraCommand = new AsyncRelayCommand(OpenPandora);

        PlayClickCommand = new AsyncRelayCommand(StartGame);
        SaveModListCommand = new AsyncRelayCommand(SaveModList);

        InstallModCommand = new AsyncRelayCommand(InstallMod);

        Logger = new UiLoggerService();
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
        SkyrimPrefsFilePath = AppConfig.Instance.SkyrimPrefsFilePath;

        PathToFnisTool = await _autoDetector.TryGetFnisExecutable();
        PathToBodySlide = await _autoDetector.TryGetBodySlideExecutable();
        PathToOutfitStudio = await _autoDetector.TryGetOutfitStudioExecutable();
        PathToNemesisTool = await _autoDetector.TryGetNemesisExecutable();
        PathToSseeditTool = await _autoDetector.TryGetSseeditExecutable();
        PathToSseeditAutoCleanTool = await _autoDetector.TryGetSseeditAutoCleanExecutable();
        PathToPandoraTool = await _autoDetector.TryGetPandoraExecutable();
        
        IsFnisAvailable = CheckForExecutable(PathToFnisTool);
        IsBodySlideAvailable = CheckForExecutable(PathToBodySlide);
        IsOutfitStudioAvailable = CheckForExecutable(PathToOutfitStudio);
        IsNemesisAvailable = CheckForExecutable(PathToNemesisTool);
        IsSseeditAvailable = CheckForExecutable(PathToSseeditTool);
        IsSseeditAutoCleanAvailable = CheckForExecutable(PathToSseeditAutoCleanTool);
        IsPandoraAvailable = CheckForExecutable(PathToPandoraTool);
        
        IsPlayAvailable = true; // no need for checking the launcher ID since I'm gonna delete it anyway
        
        IsLoadArchiveAvailable = true;
    }

    private bool CheckForExecutable(string path)
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
    
    private async Task OpenSseedit()
    {
        string relativeStudioPath = PathToSseeditTool;
        string[] dirs = relativeStudioPath.Split(Path.DirectorySeparatorChar);
        string relativeFromData = Path.Combine(
            dirs.SkipWhile(d => !d.Equals("Data", StringComparison.OrdinalIgnoreCase)).ToArray());
        await LaunchToolInProtonAsync(relativeFromData);
    }
    
    private async Task OpenSseeditAutoClean()
    {
        string relativeStudioPath = PathToSseeditAutoCleanTool;
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
    
    private async Task LaunchToolInProtonAsync(string pathToTool, Action? preLaunchSetup = null)
    {
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
    
    // refactor?
    private async Task DeleteFnisSymlinks()
    {
        string backupLauncherPath = Path.Combine(GameFolderPath, "SkyrimSELauncher_backup.exe");
        string launcherPath = Path.Combine(GameFolderPath, "SkyrimSELauncher.exe");
        string tempSymlinkDir = Path.Combine(GameFolderPath, "tools"); // temp dir inside the root
        
        // need to be careful here
        if(!File.Exists(backupLauncherPath)) return; // don't delete current one, if backup has not been found
        
        // delete symlinks
        if (Directory.Exists(tempSymlinkDir)) Directory.Delete(tempSymlinkDir);
        if (File.Exists(launcherPath)) File.Delete(launcherPath);

        // get back the original launcher
        if (File.Exists(backupLauncherPath)) File.Move(backupLauncherPath, launcherPath);
    }
    
    private async Task ReInitValues()
    {
        await AppConfig.Instance.CheckConfigStatus();
    }

    private async Task UpdateModList()
    {
        var modListM = new ModListManager();
        var updated = await modListM.UpdateModList();
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
        var modListM = new ModListManager();
        // save current state
        await modListM.SaveCurrentModListState(ModList);
        await UpdateModList();
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
        await OpenFileDirectory(SkyrimPrefsFilePath);
    }

    private async Task OpenFileDirectory(string path)
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
        var filePath = await _fileManager.SelectFile();
        if (string.IsNullOrEmpty(filePath))
        {
            await MsgBoxManager.ShowWarning("Mod archive not selected!");
            return;
        }

        IsModInstalling = true;
        IsPlayAvailable = false;
        
        Logger.Log(string.Empty); // space for better visibility
        Logger.Log($"Selected file: {filePath}");
        var fomod = new FomodManager(filePath, Logger);
        await fomod.InstallMod();
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