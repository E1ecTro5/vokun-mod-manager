using System.Collections.ObjectModel;
using VokunModManager.Models;

namespace VokunModManager.Interfaces;

public interface IModListManager
{
    public Task<ObservableCollection<Mod>?> UpdateModList();
    public Task<ObservableCollection<Plugin>?> UpdatePluginList();
    public Task SaveCurrentModListState(ObservableCollection<Mod> modList);
}