namespace VokunModManager.Interfaces;

public interface IModInstaller
{
    public Task InstallMod(string archivePath);
}