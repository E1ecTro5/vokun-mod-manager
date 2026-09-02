using Avalonia.Platform.Storage;

namespace VokunModManager.Interfaces;

public interface IFileManager
{
    public Task<string?> SelectFileAsync(FilePickerFileType[]? fileTypes = null);

    public Task<string?> SelectDirectoryAsync();
}