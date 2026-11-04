using System.Collections.ObjectModel;
using VokunModManager.Models;

namespace VokunModManager.Interfaces;

public interface IModListManager
{
    public Task<ObservableCollection<Mod>?> UpdateModList();
    public Task<ObservableCollection<ModPlugin>?> UpdateModPluginList();
    // here also add for ModPlugin in future
    public Task<ObservableCollection<SksePlugin>?> UpdateSksePluginList();
    public Task SaveCurrentModPluginsListState(ObservableCollection<ModPlugin> modList);
}