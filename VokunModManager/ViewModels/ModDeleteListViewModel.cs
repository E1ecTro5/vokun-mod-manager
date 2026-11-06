using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VokunModManager.Interfaces;
using VokunModManager.Models;

namespace VokunModManager.ViewModels;

public partial class ModDeleteListViewModel : ViewModelBase
{
    [ObservableProperty] private ObservableCollection<Mod>? _modList;
    [ObservableProperty] private Mod? _selectedMod;
    
    private IModInstaller _modInstaller;
    
    public Action? CloseAction { get; set; }
    
    public ModDeleteListViewModel(IModInstaller modInstaller, ObservableCollection<Mod>? modList)
    {
        _modInstaller = modInstaller;
        _modList = modList;
    }

    [RelayCommand]
    public async Task ConfirmDelete()
    {
        if (ModList is null || ModList.Count == 0 || SelectedMod is null) return;

        await _modInstaller.DeleteMod(SelectedMod);
        
        CloseAction?.Invoke();
    }
}