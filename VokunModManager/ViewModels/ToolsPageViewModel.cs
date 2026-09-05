using CommunityToolkit.Mvvm.ComponentModel;
using VokunModManager.Interfaces;

namespace VokunModManager.ViewModels;

public partial class ToolsPageViewModel : ViewModelBase
{
    private readonly IAutoDetector _autoDetector;
    
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

    public ToolsPageViewModel(IAutoDetector autoDetector)
    {
        _autoDetector = autoDetector;
        
        InitPaths();
    }

    private void InitPaths()
    {
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
}