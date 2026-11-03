using System.Collections.ObjectModel;
using VokunModManager.Models;

namespace VokunModManager.Interfaces;

public interface IModListManager
{
    public Task<ObservableCollection<Mod>?> UpdateModList();
    // here also add for ModPlugin in future
    public Task<ObservableCollection<SksePlugin>?> UpdateSksePluginList();
    public Task SaveCurrentModListState(ObservableCollection<Mod> modList);
}