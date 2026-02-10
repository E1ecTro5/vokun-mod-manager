using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using SharpCompress.Archives;
using SharpCompress.Common;
using VokunModManager.Models;

namespace VokunModManager.Misc;

public class FileManager
{
    private TopLevel GetOwner()
    {
        return (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)!.MainWindow!;
    }

    public DirectoryInfo LoadedArchive { get; private set; }

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

    // let's just dump all to a specific directory and work form there
    public async Task LoadArchive(string path)
    {
        string destDirectory = Path.Combine(AppConfig.Instance.BaseDirectory, "temp");
        
        await LogManager.Instance.Log("Cleaning temp folder...");
        if(Directory.Exists(destDirectory)) Directory.Delete(destDirectory, true); // probably the best way
        Directory.CreateDirectory(destDirectory);
        
        await LogManager.Instance.Log("Getting archive's files...");
        using var archive = ArchiveFactory.Open(path);

        foreach (var entry in archive.Entries)
        {
            if (entry.IsDirectory) continue;
            if (string.IsNullOrEmpty(entry.Key)) continue;

            string fullPath = Path.Combine(destDirectory, entry.Key);

            string? directory = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);

            await entry.WriteToFileAsync(fullPath);
        }

        await LogManager.Instance.Log("Archive's files have been written to folder.");
    }
    
    // research this later
    public async Task<ObservableCollection<ArchiveNode>> BuildTree(string archivePath)
    {
        var roots = new ObservableCollection<ArchiveNode>();
        var lookup = new Dictionary<string, ArchiveNode>();

        await Task.Run(() =>
        {
            using var archive = ArchiveFactory.Open(archivePath);

            foreach (var entry in archive.Entries)
            {
                if (string.IsNullOrEmpty(entry.Key)) continue;

                var parts = entry.Key.Split('/', StringSplitOptions.RemoveEmptyEntries);
                string currentPath = "";

                for (int i = 0; i < parts.Length; i++)
                {
                    currentPath = currentPath == ""
                        ? parts[i]
                        : currentPath + "/" + parts[i];

                    if (!lookup.TryGetValue(currentPath, out _))
                    {
                        bool isLastPart = i == parts.Length - 1;

                        var node = new ArchiveNode
                        {
                            Name = parts[i],
                            FullPath = currentPath,
                            IsDirectory = !isLastPart || entry.IsDirectory
                        };

                        lookup[currentPath] = node;

                        if (i == 0)
                        {
                            roots.Add(node);
                        }
                        else
                        {
                            var parentPath = currentPath.Substring(0, currentPath.LastIndexOf('/'));
                            var parent = lookup[parentPath];

                            node.Parent = parent;
                            parent.Children.Add(node);
                        }
                    }
                }
            }
        });

        return roots;
    }

    
    public List<string> GetSelectedFiles(IEnumerable<ArchiveNode> nodes)
    {
        var result = new List<string>();

        foreach (var node in nodes)
        {
            if (node.IsDirectory)
            {
                // if Directory checked get ALL
                if (node.IsChecked) result.AddRange(GetAllFiles(node));
                else result.AddRange(GetSelectedFiles(node.Children));
            }
            else if (node.IsChecked) result.Add(node.FullPath);
        }

        return result;
    }
    
    private List<string> GetAllFiles(ArchiveNode node)
    {
        var result = new List<string>();

        foreach (var child in node.Children)
        {
            if (child.IsDirectory) result.AddRange(GetAllFiles(child));
            else result.Add(child.FullPath);
        }

        return result;
    }


    public async Task InstallFiles(string archivePath, IEnumerable<ArchiveNode> tree)
    {
        using var archive = ArchiveFactory.Open(archivePath);
        var modFolderPath = Path.Combine(AppConfig.Instance.GameFolderPath, "Data");

        var entryLookup = archive.Entries
            .Where(e => !e.IsDirectory && e.Key != null)
            .ToDictionary(e => e.Key!);

        var selectedFiles = GetSelectedFiles(tree);

        foreach (var filePath in selectedFiles)
        {
            if (!entryLookup.TryGetValue(filePath, out var entry)) continue;

            string destination = Path.Combine(modFolderPath, filePath);

            await entry.WriteToFileAsync(destination, new ExtractionOptions
            {
                ExtractFullPath = true,
                Overwrite = true
            });
        }
    }
}