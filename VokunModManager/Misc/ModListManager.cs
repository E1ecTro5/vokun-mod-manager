using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using VokunModManager.Models;

namespace VokunModManager.Misc;

public class ModListManager(string modlistPath)
{
    public async Task<ObservableCollection<Mod>> GetModList()
    {
        await LogManager.Instance.Log("Initializing mod list");
        ObservableCollection<Mod> result = new();

        using (StreamReader reader = new StreamReader(modlistPath))
        {
            ushort currentIndex = 0;
            while (!reader.EndOfStream)
            {
                string line = await reader.ReadLineAsync();
                
                // ignore comments and empty lines
                if (line.StartsWith('#') || string.IsNullOrEmpty(line)) continue;

                ushort loadOrder = 0;
                bool isActive = line.StartsWith('*');
                if(!isActive) loadOrder = 0; // don't mention offed mods
                else loadOrder = ++currentIndex;
                string name = line.TrimStart('*');
                var mod = new Mod(loadOrder, name, isActive);
                result.Add(mod);
            }
        }
        
        await LogManager.Instance.Log("Mod list initialized");
        return result;
    }

    public async Task SetModList(ObservableCollection<Mod> modList)
    {
        await LogManager.Instance.Log("Setting mod list");

        // this will overwrite everything, including comments and null lines
        await using (StreamWriter writer = new StreamWriter(modlistPath))
        {
            foreach (Mod mod in modList)
            {
                await writer.WriteLineAsync(mod.ToString());
            }
        }
        
        await LogManager.Instance.Log("Mod list initialized");
    }
}