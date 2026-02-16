using Avalonia.Controls;
using VokunModManager.Views;

namespace VokunModManager.ViewModels;

public class InstallWindowViewModel : ViewModelBase
{
    private readonly StackPanel _panel;

    public InstallWindowViewModel(InstallWindow installWindow)
    {
        _panel = installWindow.MainStackPanel;
        
        AddRadioButtons();
    }
    
    // just for test. rewrite later
    private void AddRadioButtons()
    {
        var rb1 = new RadioButton();
        var rb2 = new RadioButton();
        var rb3 = new RadioButton();
        _panel.Children.Add(rb1);
        _panel.Children.Add(rb2);
        _panel.Children.Add(rb3);
    }
}