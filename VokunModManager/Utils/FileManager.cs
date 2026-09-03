using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using VokunModManager.Interfaces;

namespace VokunModManager.Utils;

public class FileManager : IFileManager
{
    private static IStorageProvider GetStorageProvider()
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime { MainWindow.StorageProvider: { } storage })
        {
            return storage;
        }

        throw new InvalidOperationException("Desktop MainWindow or StorageProvider is not initialized.");
    }

    public async Task<string?> SelectFileAsync(FilePickerFileType[]? fileTypes = null)
    {
        var storage = GetStorageProvider();

        var files = await storage.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Select a file",
            AllowMultiple = false,
            FileTypeFilter = fileTypes
        });

        using var file = files.FirstOrDefault();
        return file?.Path.LocalPath;
    }

    public async Task<string?> SelectDirectoryAsync()
    {
        var storage = GetStorageProvider();

        var folders = await storage.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "Select a folder",
            AllowMultiple = false
        });

        using var folder = folders.FirstOrDefault();
        return folder?.Path.LocalPath;
    }
}