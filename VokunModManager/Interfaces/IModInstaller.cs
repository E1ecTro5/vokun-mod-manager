using VokunModManager.Models;

namespace VokunModManager.Interfaces;

public interface IModInstaller
{
    public Task InstallMod(string archivePath);
    public Task DeleteMod(Mod mod);
}