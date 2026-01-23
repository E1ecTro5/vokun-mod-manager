using Avalonia.Controls;
using VokunModManager.ViewModels;

namespace VokunModManager.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        this.DataContext = new MainWindowViewModel();
    }
}