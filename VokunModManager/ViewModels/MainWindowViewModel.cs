using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace VokunModManager.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    [ObservableProperty] private ViewModelBase _currentPage;

    public ICommand SelectMainPageCommand { get; }
    public ICommand SelectToolsPageCommand { get; }

    // get this from DI
    private readonly MainPageViewModel _mainPageViewModel;
    private readonly ToolsPageViewModel _toolsPageViewModel;

    public MainWindowViewModel(
        MainPageViewModel mainPageViewModel,
        ToolsPageViewModel toolsPageViewModel)
    {
        _mainPageViewModel = mainPageViewModel;
        _toolsPageViewModel = toolsPageViewModel;
        _currentPage = _mainPageViewModel;

        SelectMainPageCommand = new AsyncRelayCommand(SelectMainPage);
        SelectToolsPageCommand = new AsyncRelayCommand(SelectToolsPage);
    }

    private async Task SelectMainPage()
    {
        CurrentPage = _mainPageViewModel;
    }
    
    private async Task SelectToolsPage()
    {
        CurrentPage = _toolsPageViewModel;
    }
}