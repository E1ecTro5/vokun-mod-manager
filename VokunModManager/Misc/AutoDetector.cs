namespace VokunModManager.Misc;

public class AutoDetector
{
    /// <summary>
    /// Tries to find 'Skyrim Special Edition' folder inside the Steam folder and set it to the app config.
    /// </summary>
    /// <returns>True, if folder has been found and set to the config. Otherwise, false.</returns>
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
    /// Tries to find game's Plugin.txt file, located in compatdata folder and set it to the app config.
    /// </summary>
    /// <returns>True, if file has been found and set to the config. Otherwise, false.</returns>
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
    /// Tries to get and set the config (settings) file of the game.
    /// </summary>
    /// <returns>True, if file has been found and set to the app config. Otherwise, false.</returns>
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

    /// <summary>
    /// Tries to get the GenerateFNISforUsers.exe file, if it exists.
    /// </summary>
    /// <returns>Path to the .exe file. Null if nothing found.</returns>
    public async Task<string?> TryGetFnisExecutable()
    {
        //home/epsilon/.local/share/Steam/steamapps/common/Skyrim Special Edition/Data/tools/GenerateFNIS_for_Users/GenerateFNISforUsers.exe

        string possibleLocation = string.Empty;
        string? gameFolder = AppConfig.Instance.GameFolderPath;
        if (string.IsNullOrEmpty(gameFolder)) return null;

        if (Environment.OSVersion.Platform == PlatformID.Unix)
        {
            possibleLocation = Path.Combine(gameFolder, "Data/tools/GenerateFNIS_for_Users/GenerateFNISforUsers.exe");
        }
        else if (Environment.OSVersion.Platform == PlatformID.Win32NT)
        {
            possibleLocation = Path.Combine(gameFolder, @"Data\tools\GenerateFNIS_for_Users\GenerateFNISforUsers.exe");
        }

        return File.Exists(possibleLocation) ? possibleLocation : null;
    }
    
    /// <summary>
    /// Tries to get the OutfitStudio.exe file, if it exists.
    /// </summary>
    /// <returns>Path to the .exe file. Null if nothing found.</returns>
    public async Task<string?> TryGetOutfitStudioExecutable()
    {
        //home/epsilon/.local/share/Steam/steamapps/common/Skyrim Special Edition/Data/CalienteTools/BodySlide/BodySlide.exe

        string possibleLocation = string.Empty;
        string? gameFolder = AppConfig.Instance.GameFolderPath;
        if (string.IsNullOrEmpty(gameFolder)) return null;

        if (Environment.OSVersion.Platform == PlatformID.Unix)
        {
            possibleLocation = Path.Combine(gameFolder, "Data/CalienteTools/BodySlide/OutfitStudio.exe");
        }
        else if (Environment.OSVersion.Platform == PlatformID.Win32NT)
        {
            possibleLocation = Path.Combine(gameFolder, @"Data\CalienteTools\BodySlide\OutfitStudio.exe");
        }

        return File.Exists(possibleLocation) ? possibleLocation : null;
    }
    
    /// <summary>
    /// Tries to get the BodySlide.exe file, if it exists.
    /// </summary>
    /// <returns>Path to the .exe file. Null if nothing found.</returns>
    public async Task<string?> TryGetBodySlideExecutable()
    {
        string possibleLocation = string.Empty;
        string? gameFolder = AppConfig.Instance.GameFolderPath;
        if (string.IsNullOrEmpty(gameFolder)) return null;

        if (Environment.OSVersion.Platform == PlatformID.Unix)
        {
            possibleLocation = Path.Combine(gameFolder, "Data/CalienteTools/BodySlide/BodySlide.exe");
        }
        else if (Environment.OSVersion.Platform == PlatformID.Win32NT)
        {
            possibleLocation = Path.Combine(gameFolder, @"Data\CalienteTools\BodySlide\BodySlide.exe");
        }

        return File.Exists(possibleLocation) ? possibleLocation : null;
    }
}