namespace VokunModManager.Models;

public class PathFile(string fileName, PathFile.FileType thisFileType)
{
    // maybe will add smth like "archive" or "mod folder" later...
    public enum FileType
    {
        Directory,
        File
    }

    public string FileName { get; set; } = fileName;
    public FileType ThisFileType { get; set;} = thisFileType;
}