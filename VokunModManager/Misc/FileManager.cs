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
    
    public string? CurrentPath { get; set; }

    public async Task SelectDirectory()
    {
        var storage = TopLevel.GetTopLevel(_mainWindow)?.StorageProvider;

        if (storage == null) throw new NullReferenceException("Storage provider is null");
        
        var folders = await storage.OpenFolderPickerAsync(
            new FolderPickerOpenOptions
            {
                Title = "Выберите папку",
                AllowMultiple = false
            });
        
        var folder = folders.FirstOrDefault();
        if (folder is null)
            return;

        CurrentPath = folder.Path.LocalPath;
    }
}