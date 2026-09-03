using System.Collections.ObjectModel;
using VokunModManager.Interfaces;
using VokunModManager.Models;

namespace VokunModManager.Misc;

public class ModListManager(ILoggerService logger) : IModListManager
{
    private string modlistPath = AppConfig.Instance.PluginFilePath;
    private async Task<bool> CheckForExistance()
    {
        if (string.IsNullOrEmpty(modlistPath) || !File.Exists(modlistPath))
        {
            logger.Log($"Couldn't find the mod list path.");
            return false;
        }

        return true;
    }

    public async Task<ObservableCollection<Mod>?> UpdateModList()
    {
        if (!await CheckForExistance()) return null;
        
        // Disk check should always be earlier than the plugins file.
        var modsOnDisk = await GetModsFromDisk();
        if (modsOnDisk is null || !modsOnDisk.Any())  return new ObservableCollection<Mod>();
        
        var pluginFileMods = await ReadModList();
        
        var updatedList = new ObservableCollection<Mod>();

        foreach (var diskFile in modsOnDisk)
        {
            // check if the mod is already in the plugins file.
            var savedMod = pluginFileMods.FirstOrDefault(m => string.Equals(m.Name, diskFile.Name, StringComparison.OrdinalIgnoreCase));
            if (savedMod != null)
            {
                diskFile.IsEnabled = savedMod.IsEnabled;
                diskFile.LoadOrder = savedMod.LoadOrder;
            }
            else
            {
                // new file which could not be found in Pluigns.txt
                diskFile.IsEnabled = false; // default
                diskFile.LoadOrder = 0;     // default
            }

            updatedList.Add(diskFile);
        }
        
        var sortedCollection = new ObservableCollection<Mod>(
            updatedList
                .OrderByDescending(m => m.IsEnabled)
                .ThenBy(m => m.LoadOrder)
                .ThenBy(m => m.Name)
        );
        
        await SaveCurrentModList(sortedCollection);
        
        return sortedCollection;
    }
    
    public async Task SaveCurrentModListState(ObservableCollection<Mod> modList)
    {
        var list = new ObservableCollection<Mod>();
        
        ushort order = 1;
        foreach (var mod in modList)
        {
            if (mod.IsEnabled) mod.LoadOrder = order++;
            else mod.LoadOrder = 0;
            list.Add(mod);
        }

        await SaveCurrentModList(list);
    }

    /// <summary>
    /// Reads the "Plugin.txt" file.
    /// </summary>
    /// <returns>Collection of mods, found inside the file.</returns>
    private async Task<ObservableCollection<Mod>?> ReadModList()
    {
        if (!await CheckForExistance()) return null;
        
        ObservableCollection<Mod> result = new();

        // change modlistpath to config reference?
        using (StreamReader reader = new StreamReader(modlistPath))
        {
            ushort currentIndex = 0;
            while (!reader.EndOfStream)
            {
                string? line = await reader.ReadLineAsync();

                // ignore comments and empty lines
                if (string.IsNullOrEmpty(line) || line.StartsWith('#')) continue;

                ushort loadOrder = 0;
                bool isActive = line.StartsWith('*');
                if (!isActive) loadOrder = 0; // don't mention offed mods
                else loadOrder = ++currentIndex;
                string name = line.TrimStart('*');
                var mod = new Mod(loadOrder, name, isActive);
                result.Add(mod);
            }
        }

        return result;
    }

    /// <summary>
    /// Scans the "Data" folder and tries to find any mod-related files, such as .esl, .esm and .esp...
    /// </summary>
    /// <param name="modList"></param>
    /// <returns>Collection of the found mods inside the folder.</returns>
    private async Task<ObservableCollection<Mod>?> GetModsFromDisk()
    {
        // checked on Windows first launch; needed to fix this
        string gameFolderPath = AppConfig.Instance.GameFolderPath ?? string.Empty;
        if (string.IsNullOrEmpty(gameFolderPath))
        {
            Console.WriteLine("GAMEPATH NOT FOUND");
            return null;
        }
        
        string path = Path.Combine(gameFolderPath, "Data");
        if (!Directory.Exists(path)) return null;
        
        // so, before .esp you should also include .esm and .esl files ; they shouldn't count as mod itself, but they are needed for it to be working
        // I'll refactor this later ; for now, just manual handling
        var ignoredFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Skyrim.esm", "Update.esm", "HearthFires.esm", "Dragonborn.esm", "Dawnguard.esm",
            "ccBGSSSE001-Fish.esm", "ccBGSSSE025-AdvDSGS.esm", "ccBGSSSE037-Curios.esl",
            "ccQDRSSE001-SurvivalMode.esl", "_ResourcePack.esl"
        };

        ObservableCollection<Mod> result = new ObservableCollection<Mod>();
        
        var extensions = new[] { "*.esl", "*.esm", "*.esp" };
    
        foreach (var ext in extensions)
        {
            foreach (var filePath in Directory.EnumerateFiles(path, ext))
            {
                string fileName = Path.GetFileName(filePath);
                if (ignoredFiles.Contains(fileName)) continue;

                result.Add(new Mod(0, fileName, false));
            }
        }

        return result;
    }
    
    /// <summary>
    /// Overwrites the Plugins.txt file with the ModList in arguments.
    /// </summary>
    /// <param name="modList">Relevant mod list, will be written into the file.</param>
    private async Task SaveCurrentModList(ObservableCollection<Mod> modList)
    {
        var sortedList = modList
            .OrderByDescending(m => m.IsEnabled)
            .ThenBy(m => m.LoadOrder)
            .ThenBy(m => m.Name);
        
        await using StreamWriter writer = new StreamWriter(modlistPath, false);
        foreach (Mod mod in sortedList)
        {
            await writer.WriteLineAsync(mod.ToString());
        }
    }
}