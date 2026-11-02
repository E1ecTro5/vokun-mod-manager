using CommunityToolkit.Mvvm.ComponentModel;

namespace VokunModManager.ViewModels;

public partial class InstallationProcessWindowViewModel : ViewModelBase
{
    [ObservableProperty] private string _test = "first";

    [ObservableProperty] private int _totalFiles;
    [ObservableProperty] private int _copiedFiles;
    [ObservableProperty] private int _overwrittenFiles;

    public InstallationProcessWindowViewModel()
    {
        Test = "HEY!";
    }
}