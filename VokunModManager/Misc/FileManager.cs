using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using SharpCompress.Archives;
using VokunModManager.Models;

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
        // LOCALPATH because of OS
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

    public async Task<ObservableCollection<ArchiveNode>> GetZipFiles(string path)
    {
        // no async needed?
        var items = LoadArchive(path);
        return items;
    }

    private ObservableCollection<ArchiveNode> LoadArchive(string path)
    {
        var roots = new ObservableCollection<ArchiveNode>();

        // collection with entries (everyone, like fomod/.../... and something.bsa on a single level
        using var archive = ArchiveFactory.Open(path);

        // make them like a hierarchy for easy install 
        foreach (var entry in archive.Entries.Where(e => !e.IsDirectory))
        {
            Insert(roots, entry.Key);
        }

        return roots;
    }
    
    private void Insert(ObservableCollection<ArchiveNode> roots, string fullPath)
    {
        // get all items paths
        var parts = fullPath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        
        var currentLevel = roots;
        ArchiveNode? parent = null;

        for (int i = 0; i < parts.Length; i++)
        {
            var existing = currentLevel.FirstOrDefault(x => x.Name == parts[i]);

            if (existing == null)
            {
                existing = new ArchiveNode { Name = parts[i], IsFolder = i != parts.Length - 1, Parent = parent };

                currentLevel.Add(existing);
            }

            parent = existing;
            currentLevel = existing.Children;
        }
    }

}