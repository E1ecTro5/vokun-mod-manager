using VokunModManager.Models;

namespace VokunModManager.Interfaces;

public interface IModManifestManager
{
    public Task WriteModFiles(Mod mod);
}