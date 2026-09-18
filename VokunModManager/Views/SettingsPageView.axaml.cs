using Avalonia.Controls;
using Avalonia.Interactivity;
using VokunModManager.ViewModels;

namespace VokunModManager.Views;

public partial class SettingsPageView : UserControl
{
    public SettingsPageView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private async void OnLoaded(object? sender, RoutedEventArgs e)
    {
        Loaded -= OnLoaded;

        if (DataContext is SettingsPageViewModel vm)
        {
            await vm.LateInit();
        }
    }
}