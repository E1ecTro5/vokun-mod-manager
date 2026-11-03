using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VokunModManager.Views;

namespace VokunModManager.ViewModels;

public partial class InstallationProcessWindowViewModel : ViewModelBase
{
    // main log
    [ObservableProperty] private string _logText;

    // progress bar
    [ObservableProperty] private int _totalFiles;
    [ObservableProperty] private int _processedFiles;
    
    [ObservableProperty] private bool _isInstallFinished;

    public InstallationProcessWindowViewModel()
    {
        LogText = string.Empty;
        _isInstallFinished = false;
    }
    
    [RelayCommand]
    public async Task PressOk(InstallationProcessWindow window)
    {
        window.Close();
    }
}