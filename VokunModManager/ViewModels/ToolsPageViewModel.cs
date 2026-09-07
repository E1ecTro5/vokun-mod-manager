using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VokunModManager.Interfaces;
using VokunModManager.Models;

namespace VokunModManager.ViewModels;

public partial class ToolsPageViewModel : ViewModelBase
{
    private readonly IAutoDetector _autoDetector;
    private readonly IAppConfig _appConfig;
    private readonly IFileManager _fileManager;
    private readonly IToolLauncher _toolLauncher;
    private readonly IGameStateResetter _gameStateResetter;
    private readonly ILoggerService _loggerService;
    
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
    public ICommand OpenPandoraCommand { get; }
    public ICommand OpenExternalToolCommand { get; }
    // resetter
    public ICommand SaveCurrentGameStateCommand { get; }
    public ICommand ResetGameStateCommand { get; }
    
    public ToolsPageViewModel(
        IAutoDetector autoDetector,
        IAppConfig appConfig,
        IFileManager fileManager,
        IToolLauncher toolLauncher,
        IGameStateResetter gameStateResetter,
        ILoggerService loggerService)
    {
        _autoDetector = autoDetector;
        _appConfig = appConfig;
        _fileManager = fileManager;
        _toolLauncher = toolLauncher;
        _gameStateResetter = gameStateResetter;
        _loggerService = loggerService;
        
        //tools
        OpenFnisCommand = new AsyncRelayCommand(OpenFnis);
        OpenOutfitStudioCommand = new AsyncRelayCommand(OpenOutfitStudio);
        OpenBodySlideCommand = new AsyncRelayCommand(OpenBodySlide);
        OpenNemesisCommand = new AsyncRelayCommand(OpenNemesis);
        OpenPandoraCommand = new AsyncRelayCommand(OpenPandora);

        OpenExternalToolCommand = new AsyncRelayCommand(OpenExternalTool);
        
        // resetter
        SaveCurrentGameStateCommand = new AsyncRelayCommand(SaveGameCurrentState);
        ResetGameStateCommand = new AsyncRelayCommand(ResetGameState);
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
        await _toolLauncher.LaunchInternalToolAsync(relativeFnisPath, () =>
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
        _loggerService.Log("Opening BodySlide...");
        await _toolLauncher.LaunchInternalToolAsync(relativeBodySlidePath);
    }
    
    private async Task OpenOutfitStudio()
    {
        string relativeStudioPath = Path.Combine("Data", "CalienteTools", "BodySlide", "OutfitStudio.exe");
        _loggerService.Log("Opening OutfitStudio...");
        await _toolLauncher.LaunchInternalToolAsync(relativeStudioPath);
    }

    private async Task OpenNemesis()
    {
        string relativeStudioPath = Path.Combine("Data", "Nemesis_Engine", "Nemesis Unlimited Behavior Engine.exe");
        _loggerService.Log("Opening Nemesis...");
        await _toolLauncher.LaunchInternalToolAsync(relativeStudioPath);
    }

    private async Task OpenPandora()
    {
        string relativeStudioPath = Path.Combine("Data", "Pandora Behaviour Engine+.exe");
        _loggerService.Log("Opening Pandora...");
        await _toolLauncher.LaunchInternalToolAsync(relativeStudioPath);
    }

    private async Task OpenExternalTool()
    {
        string? filePath = await _fileManager.SelectFileAsync();
        if (string.IsNullOrEmpty(filePath))
        {
            _loggerService.Log("Tool has not been selected.", LogLevel.Warning);
            return;
        }
        await _toolLauncher.LaunchExternalToolAsync(filePath);
    }
    
    private async Task SaveGameCurrentState()
    {
        if (string.IsNullOrEmpty(GameFolderPath))
        {
            _loggerService.Log("Can't save manifest, because GameFolderPath is not initialized.", LogLevel.Warning);
            return;
        }

        string manifestPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "default_manifest.json");

        // create ONLY if not exist
        if (File.Exists(manifestPath))
        {
            _loggerService.Log("Manifest already exists.");
            return;
        }

        try
        {
            _loggerService.Log("Generate clean game's manifest (default_manifest.json)...");

            await _gameStateResetter.CreateDefaultManifestAsync(GameFolderPath, CompatdataFolderPath, manifestPath);

            _loggerService.Log($"Manifest successfully created at: {manifestPath}");
        }
        catch (Exception ex)
        {
            _loggerService.Log($"Error while creating manifest: {ex.Message}", LogLevel.Error);
        }
    }

    private async Task ResetGameState()
    {
        if (string.IsNullOrEmpty(GameFolderPath))
        {
            _loggerService.Log("GameFolderPath is not initialized. Reset is unavailable.", LogLevel.Error);
            return;
        }

        try
        {
            var progress = new Progress<string>(message =>
            {
                _loggerService.Log(message);
            });
            _loggerService.Log("Resetting the game...");
            
            await _gameStateResetter.ResetToDefaultAsync(GameFolderPath, CompatdataFolderPath, progress);

            _loggerService.Log("Game state has been reset to default.");
        }
        catch (Exception ex)
        {
            _loggerService.Log($"Error while resetting the game: {ex.Message}", LogLevel.Error);
        }
        
        // need to UpdateModList() in the end...
    }
}