using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Input;
using Avalonia.Logging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VokunModManager.Interfaces;
using VokunModManager.Models;

namespace VokunModManager.ViewModels;

public partial class ToolsPageViewModel : ViewModelBase
{
    private readonly IAutoDetector _autoDetector;
    private readonly IAppConfig _appConfig;
    private readonly ILoggerService _logger;
    
    //paths
    // they all need to be displayed just to make it easier for me
    [ObservableProperty] private string? _gameFolderPath;     // Steam game folder
    [ObservableProperty] private string? _compatdataFolderPath;
    
    // tools
    // in-game directory
    [ObservableProperty] private string? _pathToFnisTool;
    [ObservableProperty] private bool _isFnisAvailable;
    [ObservableProperty] private string? _pathToBodySlide;
    [ObservableProperty] private bool _isBodySlideAvailable;
    [ObservableProperty] private string? _pathToOutfitStudio;
    [ObservableProperty] private bool _isOutfitStudioAvailable;
    [ObservableProperty] private string? _pathToNemesisTool;
    [ObservableProperty] private bool _isNemesisAvailable;
    [ObservableProperty] private string? _pathToPandoraTool;
    [ObservableProperty] private bool _isPandoraAvailable;
    
    // external
    [ObservableProperty] private string? _pathToBethIniTool;
    [ObservableProperty] private bool _isBethIniAvailable;
    [ObservableProperty] private string? _pathToXEditTool;
    [ObservableProperty] private bool _isXEditAvailable;
    [ObservableProperty] private string? _pathToXEditAutoCleanTool;
    [ObservableProperty] private bool _isXEditAutoCleanAvailable;
    
    // tools' commands
    public ICommand OpenFnisCommand { get; }
    public ICommand OpenOutfitStudioCommand { get; }
    public ICommand OpenBodySlideCommand { get; }
    public ICommand OpenNemesisCommand { get; }
    public ICommand OpenXEditCommand { get; }
    public ICommand OpenXEditAutoCleanCommand { get; }
    public ICommand OpenPandoraCommand { get; }
    public ICommand OpenBethIniCommand { get; }
    

    public ToolsPageViewModel(IAutoDetector autoDetector, IAppConfig appConfig, ILoggerService loggerService)
    {
        _autoDetector = autoDetector;
        _appConfig = appConfig;
        _logger = loggerService;
        
        //tools
        OpenFnisCommand = new AsyncRelayCommand(OpenFnis);
        OpenOutfitStudioCommand = new AsyncRelayCommand(OpenOutfitStudio);
        OpenBodySlideCommand = new AsyncRelayCommand(OpenBodySlide);
        OpenNemesisCommand = new AsyncRelayCommand(OpenNemesis);
        OpenXEditCommand = new AsyncRelayCommand(OpenXEdit);
        OpenXEditAutoCleanCommand = new AsyncRelayCommand(OpenXEditAutoClean);
        OpenPandoraCommand = new AsyncRelayCommand(OpenPandora);
        OpenBethIniCommand = new AsyncRelayCommand(OpenBethIni);
    }

    public async Task InitPaths()
    {
        GameFolderPath = _appConfig.GameFolderPath;
        CompatdataFolderPath = _appConfig.CompatdataFolderPath;
        
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
        _logger.Log("Launching Pandora.."); // just testing...
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
                    _logger.Log($"Tool executable not found at: {helperSource}", LogLevel.Error);
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
                _logger.Log($"Error launching tool in Proton: {ex.Message}", LogLevel.Error);
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
}