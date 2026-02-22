using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using VokunModManager.Models;

namespace VokunModManager.Misc;

public class ModListManager
{
    private string modlistPath = AppConfig.Instance.PluginFilePath;
    private bool CheckForExistance()
    {
        if (string.IsNullOrEmpty(modlistPath) || !File.Exists(modlistPath))
        {
            LogManager.Instance.Log("Mod list file not found!");
            //throw new FileNotFoundException("Mod list file not found!", modlistPath);
            return false;
        }

        return true;
    }

    // refactor unnecessary async/await
    public async Task<ObservableCollection<Mod>> GetModList()
    {
        if (!CheckForExistance())
        {
            await LogManager.Instance.Log("You have to set the path before getting mod list!", LogManager.LogType.Error);
            return null;
        }
        
        await LogManager.Instance.Log("Initializing mod list...");
        ObservableCollection<Mod> result = new();

        // change modlistpath to config reference?
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
        
        await LogManager.Instance.Log("Mod list initialized.");
        return result;
    }

    public async Task SetModList(ObservableCollection<Mod> modList)
    {
        await LogManager.Instance.Log("Setting mod list.");

        // this will overwrite everything, including comments and null lines
        await using (StreamWriter writer = new StreamWriter(modlistPath))
        {
            foreach (Mod mod in modList)
            {
                await writer.WriteLineAsync(mod.ToString());
            }
        }
        
        await LogManager.Instance.Log("Mod list has been set.");
    }
    
    // move to another class if needed
    public async Task<ObservableCollection<Mod>> CheckForMods(ObservableCollection<Mod> modList)
    {
        await LogManager.Instance.Log("Checking for mods...");
        string path = Path.Combine(AppConfig.Instance.GameFolderPath, "Data");
        
        // so, before .esp you should also include .esm and .esl files ; they shouldn't count as mod itself, but they are needed for it to be working
        // I'll refactor this later ; for now, just manual handling
        string[] eslItems = Directory.GetFileSystemEntries(path, "*.esl");
        string[] esmItems = Directory.GetFileSystemEntries(path, "*.esm");
        string[] espItems = Directory.GetFileSystemEntries(path, "*.esp");

        ObservableCollection<Mod> result = new ObservableCollection<Mod>();
        // 3 loops ; bad practice
        foreach (var item in eslItems)
        {
            var mod = new Mod(0, Path.GetFileName(item), false);
            // ignore basic game files ; they'll be included anyway
            if(mod.Name is "ccBGSSSE037-Curios.esl" or "ccQDRSSE001-SurvivalMode.esl" or "_ResourcePack.esl") continue; 
            if(modList.Any(m => m.Name == mod.Name)) continue; // check if it's activated
            result.Add(mod);
        }
        
        foreach (var item in esmItems)
        {
            var mod = new Mod(0, Path.GetFileName(item), false);
            // ignore basic game files ; they'll be included anyway
            if(mod.Name is "Skyrim.esm" or "Update.esm" or "HearthFires.esm" or "Dragonborn.esm" or "Dawnguard.esm" or "ccBGSSSE001-Fish.esm" or "ccBGSSSE025-AdvDSGS.esm") continue;  
            if(modList.Any(m => m.Name == mod.Name)) continue;
            result.Add(mod);
        }
        
        foreach (var item in espItems)
        {
            var mod = new Mod(0, Path.GetFileName(item), false);
            if(modList.Any(m => m.Name == mod.Name)) continue;
            result.Add(mod);
        }
        
        await LogManager.Instance.Log($"{result.Count} unactivated mods found inside the Data folder.");
        await EnableMods(result);
        return result;
    }

    private async Task EnableMods(IEnumerable<Mod> modList)
    {
        using (StreamWriter writer = new StreamWriter(modlistPath, true))
        {
            foreach (var mod in modList)
            {
                await writer.WriteLineAsync(mod.ToString());
                await LogManager.Instance.Log($"Mod included to plugin.txt: {mod.Name}");
            }
        }
    }
}