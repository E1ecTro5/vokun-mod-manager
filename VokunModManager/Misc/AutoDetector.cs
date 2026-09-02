namespace VokunModManager.Misc;

public static class AutoDetector
{
    /// <summary>
    /// Tries to find 'Skyrim Special Edition' folder inside the Steam folder and set it to the app config.
    /// </summary>
    /// <returns>Path to the game folder, if found. Null, if not.</returns>
    public static string? TryGetGameFolder()
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

        return !Directory.Exists(possiblePath) ? null : possiblePath;
    }

    /// <summary>
    /// Tries to find game's Plugin.txt file, located in compatdata folder and set it to the app config.
    /// </summary>
    /// <returns>Path to the file, if found. Null, if not.</returns>
    public static string? TryGetPluginConfig()
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

        return !File.Exists(possibleLocation) ? null : possibleLocation;
    }

    /// <summary>
    /// Tries to get and set the config (settings) file of the game.
    /// </summary>
    /// <returns>Path to the file, if found. Null, if not.</returns>
    public static string? TryGetPrefsFile()
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

        return !File.Exists(possibleLocation) ? null : possibleLocation;
    }

    // Note: all the tools below will be detected if they installed directly into the game's folder...
    
    /// <summary>
    /// Tries to get the GenerateFNISforUsers.exe file, if it exists.
    /// </summary>
    /// <returns>Path to the .exe file. Null if nothing found.</returns>
    public static string? TryGetFnisExecutable()
    {
        string? gameFolder = AppConfig.Instance.GameFolderPath;
        if (string.IsNullOrEmpty(gameFolder)) return null;

        string possibleLocation = Path.Combine(gameFolder, "Data", "tools", "GenerateFNIS_for_Users", "GenerateFNISforUsers.exe");
        
        return File.Exists(possibleLocation) ? possibleLocation : null;
    }
    
    /// <summary>
    /// Tries to get the OutfitStudio.exe file, if it exists.
    /// </summary>
    /// <returns>Path to the .exe file. Null if nothing found.</returns>
    public static string? TryGetOutfitStudioExecutable()
    {
        string? gameFolder = AppConfig.Instance.GameFolderPath;
        if (string.IsNullOrEmpty(gameFolder)) return null;

        string possibleLocation = Path.Combine(gameFolder, "Data", "CalienteTools", "BodySlide", "OutfitStudio.exe");
        
        return File.Exists(possibleLocation) ? possibleLocation : null;
    }
    
    /// <summary>
    /// Tries to get the BodySlide.exe file, if it exists.
    /// </summary>
    /// <returns>Path to the .exe file. Null if nothing found.</returns>
    public static string? TryGetBodySlideExecutable()
    {
        string? gameFolder = AppConfig.Instance.GameFolderPath;
        if (string.IsNullOrEmpty(gameFolder)) return null;

        string possibleLocation = Path.Combine(gameFolder, "Data", "CalienteTools", "BodySlide", "BodySlide.exe");
        
        return File.Exists(possibleLocation) ? possibleLocation : null;
    }
    
    /// <summary>
    /// Tries to get the Nemesis Unlimited Behavior Engine.exe file, if it exists.
    /// </summary>
    /// <returns>Path to the .exe file. Null if nothing found.</returns>
    public static string? TryGetNemesisExecutable()
    {
        string? gameFolder = AppConfig.Instance.GameFolderPath;
        if (string.IsNullOrEmpty(gameFolder)) return null;

        string possibleLocation = Path.Combine(gameFolder, "Data", "Nemesis_Engine", "Nemesis Unlimited Behavior Engine.exe");

        return File.Exists(possibleLocation) ? possibleLocation : null;
    }
    
    /// <summary>
    /// Tries to get the SSEEdit.exe file, if it exists.
    /// </summary>
    /// <returns>Path to the .exe file. Null if nothing found.</returns>
    public static string? TryGetSseeditExecutable()
    {
        string? gameFolder = AppConfig.Instance.GameFolderPath;
        if (string.IsNullOrEmpty(gameFolder)) return null;

        string dataFolder = Path.Combine(gameFolder, "Data");
        string searchFolder = Path.Combine(dataFolder, "SSEEdit");
        string? sseeditFolder = Directory.EnumerateDirectories(dataFolder)
            .FirstOrDefault(x => x.StartsWith(searchFolder) && !x.EndsWith("Cache", StringComparison.OrdinalIgnoreCase));
        if (string.IsNullOrEmpty(sseeditFolder)) return null;

        string possibleLocation = Path.Combine(sseeditFolder, "SSEEdit.exe");
        return File.Exists(possibleLocation) ? possibleLocation : null;
    }
    
    public static string? TryGetSseeditAutoCleanExecutable()
    {
        string? gameFolder = AppConfig.Instance.GameFolderPath;
        if (string.IsNullOrEmpty(gameFolder)) return null;

        string dataFolder = Path.Combine(gameFolder, "Data");
        string searchFolder = Path.Combine(dataFolder, "SSEEdit");
        string? sseeditFolder = Directory.EnumerateDirectories(dataFolder)
            .FirstOrDefault(x => x.StartsWith(searchFolder) && !x.EndsWith("Cache", StringComparison.OrdinalIgnoreCase));
        if(string.IsNullOrEmpty(sseeditFolder)) return null;

        string possibleLocation = Path.Combine(sseeditFolder, "SSEEditQuickAutoClean.exe");
        
        return File.Exists(possibleLocation) ? possibleLocation : null;
    }
    
    /// <summary>
    /// Tries to get the Pandora Behaviour Engine+.exe file, if it exists.
    /// </summary>
    /// <returns>Path to the .exe file. Null if nothing found.</returns>
    public static string? TryGetPandoraExecutable()
    {
        string? gameFolder = AppConfig.Instance.GameFolderPath;
        if (string.IsNullOrEmpty(gameFolder)) return null;

        string possibleLocation = Path.Combine(gameFolder, "Data", "Pandora Behaviour Engine+.exe");

        return File.Exists(possibleLocation) ? possibleLocation : null;
    }
    
    /// <summary>
    /// Tries to get the BethINI.exe file INSIDE THE DATA FOLDER, if it exists.
    /// </summary>
    /// <returns>Path to the .exe file. Null if nothing found.</returns>
    public static string? TryGetBethIniExecutable()
    {
        string? gameFolder = AppConfig.Instance.GameFolderPath;
        if (string.IsNullOrEmpty(gameFolder)) return null;

        string dataFolder = Path.Combine(gameFolder, "Data");
        string? bethIniFolder = Directory.EnumerateDirectories(dataFolder)
            .FirstOrDefault(x => x.StartsWith(Path.Combine(dataFolder, "BethINI")));
        if (string.IsNullOrEmpty(bethIniFolder)) return null;
        
        string possibleLocation = Path.Combine(bethIniFolder, "BethINI.exe");

        return File.Exists(possibleLocation) ? possibleLocation : null;
    }
}