using Avalonia.Controls;
using VokunModManager.ViewModels;

namespace VokunModManager.Views;

public partial class InstallationProcessWindow : Window
{
    public InstallationProcessWindow()
    {
        InitializeComponent();
        
        DataContextChanged += (sender, args) =>
        {
            if (DataContext is InstallationProcessWindowViewModel vm)
            {
                vm.PropertyChanged += (s, e) =>
                {
                    if (e.PropertyName == nameof(vm.LogText))
                    {
                        // autoscroll to the end
                        LogTextBox.CaretIndex = LogTextBox.Text?.Length ?? 0;
                    }
                };
            }
        };
    }
}