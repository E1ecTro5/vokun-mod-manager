using Avalonia.Controls;
using Avalonia.Interactivity;
using VokunModManager.ViewModels;

namespace VokunModManager.Views;

public partial class MainPageView : UserControl
{
    public MainPageView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private async void OnLoaded(object? sender, RoutedEventArgs e)
    {
        Loaded -= OnLoaded;

        if (DataContext is MainPageViewModel vm)
        {
            await vm.UpdateAll();
        }
    }
}