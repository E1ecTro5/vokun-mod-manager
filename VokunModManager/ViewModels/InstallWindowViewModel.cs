using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VokunModManager.Models;

namespace VokunModManager.ViewModels;

public partial class InstallWindowViewModel : ViewModelBase
{
    private readonly TaskCompletionSource<List<PluginOption?>>? _tcs;
    private readonly string _type;

    [ObservableProperty] private string _groupName;
    [ObservableProperty] private List<PluginOption> _options;

    public bool IsSingleSelect => _type == "SelectExactlyOne" || _type == "SelectAtMostOne";
    public bool IsMultiSelect => _type == "SelectAny";
    
    public ICommand SelectCommand { get; }

    // ctor WITHOUT the window link
    public InstallWindowViewModel(FileGroup group, TaskCompletionSource<List<PluginOption?>> tcs)
    {
        _tcs = tcs;
        GroupName = group.Name;
        _type = group.Type;
        Options = group.Plugins.ToList();

        foreach (var option in _options)
        {
            option.Description = option.Description.Trim();
        }

        SelectCommand = new RelayCommand(SelectButton);
    }
    
    private void SelectButton()
    {
        // get all the selected files
        var selectedOptions = Options.Where(x => x.IsSelected).Cast<PluginOption?>().ToList();
        
        _tcs?.TrySetResult(selectedOptions);
    }
}