using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;

namespace VokunModManager.Misc;

public class FileManager
{
    private TopLevel GetOwner()
    {
        return (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)!.MainWindow!;
    }

    // make it all static?
    
    public async Task<string?> SelectFile()
    {
        var storage = TopLevel.GetTopLevel(GetOwner())?.StorageProvider;

        if (storage == null) throw new NullReferenceException("Storage provider is null");
        
        var files = await storage.OpenFilePickerAsync(
            new FilePickerOpenOptions()
            {
                Title = "Select a file",
                AllowMultiple = false
            });
        
        var file = files.FirstOrDefault();
        // LOCALPATH because of OS
        return file?.Path.LocalPath;
    }

    public async Task<string?> SelectDirectory()
    {
        var storage = TopLevel.GetTopLevel(GetOwner())?.StorageProvider;

        if (storage == null) throw new NullReferenceException("Storage provider is null");
        
        var folders = await storage.OpenFolderPickerAsync(
            new FolderPickerOpenOptions
            {
                Title = "Select a  folder",
                AllowMultiple = false
            });
        
        var folder = folders.FirstOrDefault();
        // LOCALPATH because of OS
        return folder?.Path.LocalPath;
    }
    
    // methods for autodetecting
    // btw, they shouldn't work on Windows since I use '/' there
    // I'll get this done one day :)
    
    /// <summary>
    /// Tries to find Skyrim's SE folder inside the Steam folder.
    /// </summary>
    /// <returns>True, if folder has been found and set. Otherwise, false.</returns>
    public async Task<bool> TryGetGameFolder()
    {
        string userFolder = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        string possiblePath = Path.Combine(userFolder, ".local/share/Steam/steamapps/common/Skyrim Special Edition");

        if (!Directory.Exists(possiblePath)) return false;
        
        await AppConfig.Instance.UpdateConfig(AppConfig.ConfigType.GameFolderPath, possiblePath);
        return true;
    }

    /// <summary>
    /// Tries to find game's Plugin.txt file, located in compatdata folder.
    /// </summary>
    /// <returns>True, if file has been found and set. Otherwise, false.</returns>
    public async Task<bool> TryGetPluginConfig()
    {
        string userFolder = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        // ID 489830 is specifically for Skyrim Special Edition
        string possiblePath = Path.Combine(userFolder, ".local/share/Steam/steamapps/compatdata/489830/pfx/drive_c/users/steamuser/AppData/Local/Skyrim Special Edition/Plugins.txt");

        if (!File.Exists(possiblePath)) return false;
        
        await AppConfig.Instance.UpdateConfig(AppConfig.ConfigType.PluginFilePath, possiblePath);
        return true;
    }

    /// <summary>
    /// Tries to find Steam's shortcuts.vdf file, needed for detecting the launcher ID
    /// </summary>
    /// <returns>True, if file has been found and set. Otherwise, false.</returns>
    public async Task<bool> TryGetVdfConfig()
    {
        //.local/share/Steam/userdata/392653044/config/shortcuts.vdf
        string userFolder = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        // ID 489830 is specifically for Skyrim Special Edition
        string userdataFolder = Path.Combine(userFolder, ".local/share/Steam/userdata");

        if (!Directory.Exists(userdataFolder)) return false;

        var dirs = Directory.GetDirectories(userdataFolder);

        if (dirs.Length == 0) return false;
        
        // we don't exactly know which of them
        if (dirs.Length > 1) return false;

        var userId = dirs.First();
        string possiblePath = Path.Combine(userdataFolder, userId, "config/shortcuts.vdf");
        
        if (!File.Exists(possiblePath)) return false;
        
        await AppConfig.Instance.UpdateConfig(AppConfig.ConfigType.VdfConfigPath, possiblePath);
        return true;
    }
}