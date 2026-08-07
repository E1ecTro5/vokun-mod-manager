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
    
    // methods for auto-detecting
    // btw, they shouldn't work on Windows since I use '/' there
    // I'll get this done one day
    
    /// <summary>
    /// Tries to find 'Skyrim Special Edition' folder inside the Steam folder.
    /// </summary>
    /// <returns>True, if folder has been found and set. Otherwise, false.</returns>
    public async Task<bool> TryGetGameFolder()
    {
        string possiblePath = string.Empty;

        if (Environment.OSVersion.Platform == PlatformID.Unix)
        {
            string userFolder = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            possiblePath = Path.Combine(userFolder, ".local/share/Steam/steamapps/common/Skyrim Special Edition");
        }
        else if (Environment.OSVersion.Platform == PlatformID.Win32NT)
        {
            var programFilesPath = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
            possiblePath = Path.Combine(programFilesPath, @"Steam\steamapps\common\Skyrim Special Edition");
        }

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
        string appdataFolder = string.Empty;
        string possibleLocation = string.Empty;

        if (Environment.OSVersion.Platform == PlatformID.Unix)
        {
            appdataFolder = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            possibleLocation = Path.Combine(appdataFolder, ".local/share/Steam/steamapps/compatdata/489830/pfx/drive_c/users/steamuser/AppData/Local/Skyrim Special Edition/Plugins.txt");
        }
        else if (Environment.OSVersion.Platform == PlatformID.Win32NT)
        {
            appdataFolder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            possibleLocation = Path.Combine(appdataFolder, @"Skyrim Special Edition\Plugins.txt");
        }
        if (!File.Exists(possibleLocation)) return false;
        
        await AppConfig.Instance.UpdateConfig(AppConfig.ConfigType.PluginFilePath, possibleLocation);
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
    
    /// <summary>
    /// Tries to find the non-steam game ID, added to Steam. Basically, this is used only for finding the skse64_launcher.exe on linux.
    /// No need to this on Windows.
    /// </summary>
    /// <param name="gameFolderPath">Skyrim SE Folder path.</param>
    /// <returns>The ID of the .exe file. Used in StartGame method in main ViewModel as the ID of a launchable steam app.</returns>
    public async Task<ulong> GetGameId(string gameFolderPath)
    {
        if (string.IsNullOrEmpty(gameFolderPath))
        {
            await MsgBoxManager.ShowWarning("GameFolderPath is missing!");
            return 0;
        }
        
        string path = Path.Combine(gameFolderPath, "skse64_loader.exe");
        ulong result = 0;
        
        try
        {
            result = await GameIdFinder.GetLongId(path);
        }
        catch (Exception ex)
        {
            await MsgBoxManager.ShowWarning($"Couldn't identify GameID. Exception message: {ex.Message}");
        }

        return result;
    }
    
    /// <summary>
    /// Just extracts numbers from folder's name if possible. 
    /// </summary>
    /// <param name="path">Folder, number in which will be read.</param>
    /// <returns></returns>
    public async Task<ulong> TryGetValueFromDirectory(string path)
    {
        if(ulong.TryParse(path, out ulong result)) return result;
        return 0; // always check for 0 like you check for null or empty
    }

    /// <summary>
    /// Tries to get the config (settings) file of the game.
    /// </summary>
    /// <returns>Path to the SkyrimPrefs.ini file.</returns>
    public async Task<string?> GetGameConfig()
    {
        string pathToDocs = string.Empty;
        string possibleLocation = string.Empty;

        if (Environment.OSVersion.Platform == PlatformID.Unix)
        {
            pathToDocs = ".local/share/Steam/steamapps/compatdata/489830/pfx/drive_c/users/steamuser/My Documents/My Games/Skyrim Special Edition/SkyrimPrefs.ini";
            string possibleLoc = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), pathToDocs);
        }
        else if (Environment.OSVersion.Platform == PlatformID.Win32NT)
        {
            pathToDocs = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            possibleLocation = Path.Combine(pathToDocs, @"Documents\My Games\Skyrim Special Edition\SkyrimPrefs.ini");
        }

        if(File.Exists(possibleLocation)) return possibleLocation;
        return null;
    }
}