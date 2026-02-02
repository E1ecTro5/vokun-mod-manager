namespace VokunModManager.Models;

public class PathFile
{
    // maybe will add smth like "archive" or "mod folder" later...
    public enum FileType
    {
        Directory,
        File
    }

    public string FileName { get; set; }
    public FileType ThisFileType { get; set;}

    public PathFile(string fileName, FileType thisFileType)
    {
        FileName = fileName;
        ThisFileType = thisFileType;
    }
}