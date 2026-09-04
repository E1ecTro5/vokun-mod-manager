using System.Reflection;
using System.Text.Json;
using VokunModManager.Interfaces;

namespace VokunModManager.Utils;

public class GameStateResetter : IGameStateResetter
{
    private const string ResourceName = "VokunModManager.Resources.default_manifest.json";

    public record GameManifest(
        List<string> GameFiles,
        List<string> AppDataFiles
    );

    /// <summary>
    /// Generate JSON-manifest of the game (its default state).
    /// </summary>
    public async Task CreateDefaultManifestAsync(string gameFolderPath, string? compatDataPath, string outputPath)
    {
        var gameFiles = GetRelativeFilePaths(gameFolderPath);

        var appDataFiles = new List<string>();
        string? appDataPath = GetAppDataPath(compatDataPath);

        if (!string.IsNullOrEmpty(appDataPath) && Directory.Exists(appDataPath))
        {
            appDataFiles = GetRelativeFilePaths(appDataPath);
        }

        var manifest = new GameManifest(gameFiles, appDataFiles);

        var options = new JsonSerializerOptions { WriteIndented = true };
        await using FileStream fs = File.Create(outputPath);
        await JsonSerializer.SerializeAsync(fs, manifest, options);
    }

    /// <summary>
    /// Reset the game to the default state.
    /// </summary>
    public async Task ResetToDefaultAsync(string gameFolderPath, string? compatDataPath, IProgress<string>? progress = null)
    {
        var manifest = await LoadManifestAsync();
        if (manifest == null) throw new InvalidOperationException("Couldn't load game's manifest.");

        var defaultGameSet = new HashSet<string>(manifest.GameFiles, StringComparer.OrdinalIgnoreCase);
        var defaultAppDataSet = new HashSet<string>(manifest.AppDataFiles, StringComparer.OrdinalIgnoreCase);

        await Task.Run(() =>
        {
            // Cleaning game folder
            if (Directory.Exists(gameFolderPath))
            {
                progress?.Report("Restoring original game launcher...");
                RestoreOriginalLauncher(gameFolderPath, progress);
                
                progress?.Report("Cleaning files in game's folder...");
                CleanupDirectory(gameFolderPath, defaultGameSet, progress);
            }

            // cleaning AppData in compatdata
            string? appDataPath = GetAppDataPath(compatDataPath);
            if (!string.IsNullOrEmpty(appDataPath) && Directory.Exists(appDataPath))
            {
                progress?.Report("Cleaning AppData...");
                CleanupDirectory(appDataPath, defaultAppDataSet, progress);
            }
        });
    }
    
    /// <summary>
    /// Returns the original SkyrimSELauncher.exe from backup.
    /// </summary>
    private void RestoreOriginalLauncher(string gameFolderPath, IProgress<string>? progress)
    {
        string launcherPath = Path.Combine(gameFolderPath, "SkyrimSELauncher.exe");
        string backupPath = Path.Combine(gameFolderPath, "SkyrimSELauncher_backup.exe");
        string helperConfigPath = Path.Combine(gameFolderPath, "vokun_tool_config.txt");

        try
        {
            if (File.Exists(backupPath))
            {
                if (File.Exists(launcherPath))
                {
                    File.Delete(launcherPath);
                }

                File.Move(backupPath, launcherPath);
                progress?.Report("Original launcher restored from backup.");
            }

            // delete config for ToolLauncher.exe
            if (File.Exists(helperConfigPath))
            {
                File.Delete(helperConfigPath);
            }
        }
        catch (Exception ex)
        {
            progress?.Report($"Failed to restore launcher: {ex.Message}");
        }
    }

    private void CleanupDirectory(string basePath, HashSet<string> defaultFiles, IProgress<string>? progress)
    {
        var currentFiles = Directory.EnumerateFiles(basePath, "*", SearchOption.AllDirectories);

        foreach (var filePath in currentFiles)
        {
            string relativePath = NormalizePath(Path.GetRelativePath(basePath, filePath));

            // ignore/don't delete saves
            if (relativePath.StartsWith("Saves/", StringComparison.OrdinalIgnoreCase)) continue;

            if (defaultFiles.Contains(relativePath)) continue;
            progress?.Report($"Deleting: {relativePath}");
            File.Delete(filePath);
        }

        RemoveEmptySubdirectories(basePath);
    }

    private List<string> GetRelativeFilePaths(string basePath)
    {
        if (!Directory.Exists(basePath))
            return new List<string>();

        return Directory.EnumerateFiles(basePath, "*", SearchOption.AllDirectories)
            .Select(fullPath => NormalizePath(Path.GetRelativePath(basePath, fullPath)))
            .ToList();
    }

    private static string NormalizePath(string path)
    {
        return path.Replace('\\', '/');
    }

    private static string? GetAppDataPath(string? compatDataPath)
    {
        if (string.IsNullOrWhiteSpace(compatDataPath)) return null;

        return Path.Combine(compatDataPath, "pfx/drive_c/users/steamuser/AppData/Local/Skyrim Special Edition");
    }

    private async Task<GameManifest?> LoadManifestAsync()
    {
        var assembly = Assembly.GetExecutingAssembly();
        await using Stream? stream = assembly.GetManifestResourceStream(ResourceName);
        
        // fallback
        if (stream == null)
        {
            string localFallback = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "default_manifest.json");
            if (!File.Exists(localFallback)) return null;
            await using var fileStream = File.OpenRead(localFallback);
            return await JsonSerializer.DeserializeAsync<GameManifest>(fileStream);
        }

        return await JsonSerializer.DeserializeAsync<GameManifest>(stream);
    }

    private void RemoveEmptySubdirectories(string startLocation)
    {
        foreach (var directory in Directory.GetDirectories(startLocation))
        {
            RemoveEmptySubdirectories(directory);
            if (Directory.EnumerateFileSystemEntries(directory).Any()) continue;
            
            try
            {
                Directory.Delete(directory, false);
            }
            catch (IOException) { /* ignore if busy */ }
        }
    }
}