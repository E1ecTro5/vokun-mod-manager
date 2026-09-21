using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace VokunModManager.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsMainPageSelected))]
    [NotifyPropertyChangedFor(nameof(IsToolsPageSelected))]
    [NotifyPropertyChangedFor(nameof(IsSettingsPageSelected))]
    private ViewModelBase _currentPage;

    public ICommand SelectMainPageCommand { get; }
    public ICommand SelectToolsPageCommand { get; }
    public ICommand SelectSettingsPageCommand { get; }

    // get this from DI
    private readonly MainPageViewModel _mainPageViewModel;
    private readonly ToolsPageViewModel _toolsPageViewModel;
    private readonly SettingsPageViewModel _settingsPageViewModel;

    public bool IsMainPageSelected => CurrentPage is MainPageViewModel;
    public bool IsToolsPageSelected => CurrentPage is ToolsPageViewModel;
    public bool IsSettingsPageSelected => CurrentPage is SettingsPageViewModel;

    public MainWindowViewModel(
        MainPageViewModel mainPageViewModel,
        ToolsPageViewModel toolsPageViewModel,
        SettingsPageViewModel settingsPageViewModel)
    {
        _mainPageViewModel = mainPageViewModel;
        _toolsPageViewModel = toolsPageViewModel;
        _settingsPageViewModel = settingsPageViewModel;
        _currentPage = _mainPageViewModel;

        SelectMainPageCommand = new AsyncRelayCommand(SelectMainPage);
        SelectToolsPageCommand = new AsyncRelayCommand(SelectToolsPage);
        SelectSettingsPageCommand = new AsyncRelayCommand(SelectSettingsPage);
    }

    private async Task SelectMainPage()
    {
        CurrentPage = _mainPageViewModel;
    }
    
    private async Task SelectToolsPage()
    {
        CurrentPage = _toolsPageViewModel;
    }
    
    private async Task SelectSettingsPage()
    {
        CurrentPage = _settingsPageViewModel;
    }
}