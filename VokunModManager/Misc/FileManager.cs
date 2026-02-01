using System;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;

namespace VokunModManager.Misc;

public class FileManager
{
    //private static Window? _mainWindow;

    public FileManager()
    {
        //_mainWindow = mainWindow;
        if (GetOwner().StorageProvider is null)
            throw new InvalidOperationException("Window is not initialized yet. Call after Opened event.");
    }
    
    private TopLevel GetOwner()
    {
        return (Application.Current.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)!.MainWindow!;
    }

    public async Task<string> SelectFile()
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
        if (file is null)
            return null;

        // LOCALPATH because of OS
        return file.Path.LocalPath;
    }

    public async Task<string> SelectDirectory()
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
        if (folder is null)
            return null;

        // LOCALPATH because of OS
        return folder.Path.LocalPath;
    }
}