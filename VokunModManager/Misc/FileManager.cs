using System;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;

namespace VokunModManager.Misc;

public class FileManager
{
    private static Window? _mainWindow;

    public FileManager(Window mainWindow)
    {
        _mainWindow = mainWindow;
    }

    public async Task<string> SelectFile()
    {
        var storage = TopLevel.GetTopLevel(_mainWindow)?.StorageProvider;

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

        return file.Path.AbsolutePath;
    }

    public async Task<string> SelectDirectory()
    {
        var storage = TopLevel.GetTopLevel(_mainWindow)?.StorageProvider;

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

        return folder.Path.AbsolutePath;
    }
}