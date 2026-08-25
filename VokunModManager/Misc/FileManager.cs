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
        
        file?.Dispose();
        
        // LOCAL PATH because of OS
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
        string possibleLocation = string.Empty;

        if (Environment.OSVersion.Platform == PlatformID.Unix)
        {
            var appdataFolder = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            possibleLocation = Path.Combine(appdataFolder, ".local/share/Steam/steamapps/compatdata/489830/pfx/drive_c/users/steamuser/AppData/Local/Skyrim Special Edition/Plugins.txt");
        }
        else if (Environment.OSVersion.Platform == PlatformID.Win32NT)
        {
            var appdataFolder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            possibleLocation = Path.Combine(appdataFolder, @"Skyrim Special Edition\Plugins.txt");
        }
        if (!File.Exists(possibleLocation)) return false;
        
        await AppConfig.Instance.UpdateConfig(AppConfig.ConfigType.PluginFilePath, possibleLocation);
        return true;
    }

    /// <summary>
    /// Tries to get the config (settings) file of the game.
    /// </summary>
    /// <returns>Path to the SkyrimPrefs.ini file.</returns>
    public async Task<bool> TryGetPrefsFile()
    {
        string possibleLocation = string.Empty;

        if (Environment.OSVersion.Platform == PlatformID.Unix)
        {
            var pathToDocs = ".local/share/Steam/steamapps/compatdata/489830/pfx/drive_c/users/steamuser/My Documents/My Games/Skyrim Special Edition/SkyrimPrefs.ini";
            possibleLocation = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), pathToDocs);
        }
        else if (Environment.OSVersion.Platform == PlatformID.Win32NT)
        {
            var pathToDocs = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            possibleLocation = Path.Combine(pathToDocs, @"Documents\My Games\Skyrim Special Edition\SkyrimPrefs.ini");
        }

        if(!File.Exists(possibleLocation)) return false;
        await AppConfig.Instance.UpdateConfig(AppConfig.ConfigType.SkyrimPrefsFilePath, possibleLocation);
        return true;
    }
}