using Avalonia.Controls;
using Avalonia.Interactivity;
using VokunModManager.ViewModels;

namespace VokunModManager.Views;

public partial class ModDeleteListWindow : Window
{
    private readonly ModDeleteListViewModel _viewModel;

    public ModDeleteListWindow(ModDeleteListViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = _viewModel;

        _viewModel.CloseAction = () => Close();
    }
    
    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}