namespace VokunModManager.Models;

public class InstallationProgressReport
{
    public string CurrentLogMessage { get; set; } = string.Empty;
    public int TotalFiles { get; set; }
    public int ProcessedFiles { get; set; }
}