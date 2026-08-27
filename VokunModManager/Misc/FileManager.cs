using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;

namespace VokunModManager.Misc;

public class FileManager
{
    private TopLevel GetOwner()
    {
        return (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)!.MainWindow!;
    }

    // make it all static?
    
    public async Task<string?> SelectFile()
    {
        var storage = TopLevel.GetTopLevel(GetOwner())?.StorageProvider;

        if (storage == null) throw new NullReferenceException("Storage provider is null");
        
        var files = await storage.OpenFilePickerAsync(
            new FilePickerOpenOptions()
            {
                Title = "Select a file",
                AllowMultiple = false
            });
        
        var file = files.FirstOrDefault();
        
        file?.Dispose();
        
        // LOCAL PATH because of OS
        return file?.Path.LocalPath;
    }

    public async Task<string?> SelectDirectory()
    {
        var storage = TopLevel.GetTopLevel(GetOwner())?.StorageProvider;

        if (storage == null) throw new NullReferenceException("Storage provider is null");
        
        var folders = await storage.OpenFolderPickerAsync(
            new FolderPickerOpenOptions
            {
                Title = "Select a  folder",
                AllowMultiple = false
            });
        
        var folder = folders.FirstOrDefault();
        // LOCALPATH because of OS
        return folder?.Path.LocalPath;
    }
}