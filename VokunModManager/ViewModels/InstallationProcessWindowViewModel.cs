using CommunityToolkit.Mvvm.ComponentModel;

namespace VokunModManager.ViewModels;

public partial class InstallationProcessWindowViewModel : ViewModelBase
{
    // main log
    [ObservableProperty] private string _logText;

    // progress bar
    [ObservableProperty] private int _totalFiles;
    [ObservableProperty] private int _processedFiles;

    public InstallationProcessWindowViewModel()
    {
        LogText = string.Empty;
    }
}