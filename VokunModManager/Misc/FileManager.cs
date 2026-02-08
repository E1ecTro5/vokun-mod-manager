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
        // split the full path into parts by '/' to separate folders and file name
        var parts = fullPath.Split('/', StringSplitOptions.RemoveEmptyEntries);

        // start at the root level of the tree
        var currentLevel = roots;
        ArchiveNode currentNode = null;

        // loop through each part of the path
        for (int i = 0; i < parts.Length; i++)
        {
            // check if a node with the current part name already exists at this level
            var existing = currentLevel.FirstOrDefault(x => x.Name == parts[i]);

            if (existing == null)
            {
                // check is it's the last ; if yes than it's the file
                existing = new ArchiveNode { Name = parts[i], IsFolder = i != parts.Length - 1 };

                currentLevel.Add(existing);
            }

            currentLevel = existing.Children;
        }
    }
}