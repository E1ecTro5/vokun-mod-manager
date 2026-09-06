using Avalonia.Controls;
using Avalonia.Interactivity;
using VokunModManager.ViewModels;

namespace VokunModManager.Views;

public partial class ToolsPageView : UserControl
{
    public ToolsPageView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private async void OnLoaded(object? sender, RoutedEventArgs e)
    {
        Loaded -= OnLoaded;

        if (DataContext is ToolsPageViewModel vm)
        {
            await vm.InitPaths();
        }
    }
}