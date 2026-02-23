using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VokunModManager.Models;
using VokunModManager.Views;

namespace VokunModManager.ViewModels;

public partial class InstallWindowViewModel : ViewModelBase
{
    public enum InstallType
    {
        SelectExactlyOne,
        SelectAny
    }

    private readonly StackPanel _panel;
    private readonly List<PluginOption> _options;
    private readonly TaskCompletionSource<List<PluginOption>>? _tcs;
    private readonly  InstallType _type;

    [ObservableProperty] private string _textBlockContent;
    
    public ICommand SelectCommand { get; }

    public InstallWindowViewModel(InstallType type, IEnumerable<PluginOption> options, TaskCompletionSource<List<PluginOption?>> tcs, InstallWindow installWindow)
    {
        _panel = installWindow.MainStackPanel;
        _options = options.ToList();
        _tcs = tcs;
        _type = type;
        
        SelectCommand = new AsyncRelayCommand(SelectButton);
    }

    public async Task Init()
    {
        _panel.Children.Clear();

        switch (_type)
        {
            case InstallType.SelectExactlyOne:
                await SetRadioButtons();
                break;
            case InstallType.SelectAny:
                await SetCheckboxes();
                break;
        }
    }

    private async Task SetRadioButtons()
    {
        foreach (var rb in _options)
        {
            var name = rb.Name;
            var desc = rb.Description;

            _panel.Children.Add(new RadioButton() { Content = name });
            _panel.Children.Add(new TextBlock() { Text = desc });
        }
    }

    private async Task SetCheckboxes()
    {
        foreach (var cb in _options)
        {
            var name = cb.Name;
            var desc = cb.Description;

            _panel.Children.Add(new CheckBox() { Content = name });
            _panel.Children.Add(new TextBlock() { Text = desc });
        }
    }
    
    private async Task SelectButton()
    {
        ToggleButton? selected = null;
        switch (_type)
        {
            case InstallType.SelectExactlyOne:
                selected = (RadioButton)_panel.Children.OfType<RadioButton>().FirstOrDefault(rb => rb.IsChecked == true);
                break;
            case InstallType.SelectAny:
                selected = (CheckBox)_panel.Children.OfType<CheckBox>().FirstOrDefault(rb => rb.IsChecked == true);
                break;
        }

        if (selected == null) _tcs?.TrySetResult(null);
        
        var selectedOption = _options.FindAll(x => x.Name == selected.Content.ToString());

        if (selectedOption != null)
        {
            // release the thread
            _tcs?.TrySetResult(selectedOption);
        }
    }
}