using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using VokunModManager.ViewModels;

namespace VokunModManager.Views;

public partial class InstallWindow : Window
{
    public InstallWindow()
    {
        InitializeComponent();
        DataContext = new InstallWindowViewModel(this);
    }
}