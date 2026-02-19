using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VokunModManager.Misc;
using VokunModManager.Models;
using VokunModManager.Views;

namespace VokunModManager.ViewModels;

public partial class InstallWindowViewModel : ViewModelBase
{
    private readonly StackPanel _panel;
    private readonly InstallWindow _window;
    private readonly List<PluginOption> _options;
    private readonly TaskCompletionSource<PluginOption>? _tcs;


    [ObservableProperty] private string _textBlockContent;
    
    public ICommand SelectCommand { get; }

    public InstallWindowViewModel(InstallWindow installWindow, IEnumerable<PluginOption> options, TaskCompletionSource<PluginOption?> tcs)
    {
        _window = installWindow;
        _panel = _window.MainStackPanel;
        _options = options.ToList();
        _tcs = tcs;
        
        SelectCommand = new AsyncRelayCommand(SelectButton);

        SetRadioButtons();
    }

    private async Task SetRadioButtons()
    {
        _panel.Children.Clear();
        
        foreach (var rb in _options)
        {
            var name = rb.Name;
            var desc = rb.Description;

            _panel.Children.Add(new RadioButton() { Content = name });
            _panel.Children.Add(new TextBlock() { Text = desc });
        }
    }

    public async Task<PluginOption> GetSelectedPlugin()
    {
        var rbs = _panel.Children.OfType<RadioButton>();
        var selected = rbs.Where(x => (bool)x.IsChecked!).Select(x => x.Content).FirstOrDefault();
        var result = _options.First(x => string.Equals(x.Name, selected));

        return result;
    }
    
    private async Task SelectButton()
    {
        var selectedRadioButton = _panel.Children.OfType<RadioButton>().FirstOrDefault(rb => rb.IsChecked == true);
        if (selectedRadioButton == null) return;
        
        var selectedOption = _options.FirstOrDefault(x => x.Name == selectedRadioButton.Content.ToString());

        if (selectedOption != null)
        {
            // release the thread
            _tcs?.TrySetResult(selectedOption);
        }
    }
}