using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VokunModManager.Models;

namespace VokunModManager.ViewModels;

public partial class InstallWindowViewModel : ViewModelBase
{
    private readonly TaskCompletionSource<List<PluginOption?>>? _tcs;
    [ObservableProperty] private string _type;

    [ObservableProperty] private string _groupName;
    [ObservableProperty] private List<PluginOption> _options;

    public bool IsSingleSelect => Type is "SelectExactlyOne" or "SelectAtMostOne";
    public bool IsMultiSelect => Type is "SelectAny" or "SelectAll" or "SelectAtLeastOne";
    
    public ICommand SelectCommand { get; }

    // ctor WITHOUT the window link
    public InstallWindowViewModel(FileGroup group, TaskCompletionSource<List<PluginOption?>> tcs)
    {
        _tcs = tcs;
        GroupName = group.Name;
        Type = group.Type;
        Options = group.Plugins.ToList();

        foreach (var option in _options) option.Description = option.Description.Trim();

        // select by default specifically to this type
        if (Type == "SelectExactlyOne" && !Options.Any(x => x.IsSelected) && Options.Count > 0) Options[0].IsSelected = true;
        
        SelectCommand = new RelayCommand(SelectButton);
    }
    
    private void SelectButton()
    {
        var selectedCount = Options.Count(x => x.IsSelected);
        
        if (Type == "SelectExactlyOne" && selectedCount != 1) return;
        if (Type == "SelectAtLeastOne" && selectedCount < 1) return;
        if (Type == "SelectAtMostOne" && selectedCount > 1) return;
        
        // get all the selected files
        var selectedOptions = Options.Where(x => x.IsSelected).Cast<PluginOption?>().ToList();
        
        _tcs?.TrySetResult(selectedOptions);
    }
    
    public void HandleWindowClosed()
    {
        _tcs?.TrySetCanceled(); 
    }
}