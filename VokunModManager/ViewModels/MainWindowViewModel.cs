using CommunityToolkit.Mvvm.ComponentModel;

namespace VokunModManager.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    [ObservableProperty] 
    private ViewModelBase _currentPage;

    // get this from DI
    private readonly MainPageViewModel _mainPageViewModel;

    public MainWindowViewModel(MainPageViewModel mainPageViewModel)
    {
        _mainPageViewModel = mainPageViewModel;
        _currentPage = _mainPageViewModel;
    }
}