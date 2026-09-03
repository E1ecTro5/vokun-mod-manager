namespace VokunModManager.Interfaces;

public interface IAutoDetector
{
    public string? TryGetGameFolder();
    public string? TryGetPluginConfig();
    public string? TryGetPrefsFile();
    public string? TryGetFnisExecutable();
    public string? TryGetOutfitStudioExecutable();
    public string? TryGetBodySlideExecutable();
    public string? TryGetNemesisExecutable();
    public string? TryGetSseeditExecutable();
    public string? TryGetSseeditAutoCleanExecutable();
    public string? TryGetPandoraExecutable();
    public string? TryGetBethIniExecutable();
}