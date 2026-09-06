namespace VokunModManager.Interfaces;

public interface IToolLauncher
{
    Task LaunchInternalToolAsync(string relativeToolPath, Action? preLaunchSetup = null);
    Task LaunchExternalToolAsync(string executablePath, string? arguments = null);
}