namespace VokunModManager.Interfaces;

public interface IGameStateResetter
{
    Task CreateDefaultManifestAsync(string gameFolderPath, string? compatDataPath, string outputPath);
    Task ResetToDefaultAsync(string gameFolderPath, string? compatDataPath, IProgress<string>? progress = null);
}