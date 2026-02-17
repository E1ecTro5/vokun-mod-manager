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

    private List<string> GetSelectedFiles(IEnumerable<ArchiveNode> nodes)
    {
        var result = new List<string>();
        CollectSelectedFiles(nodes, result);
        return result;
    }
    
    private void CollectSelectedFiles(IEnumerable<ArchiveNode> nodes, List<string> result)
    {
        foreach (var node in nodes)
        {
            if (node.IsDirectory)
            {
                // if Directory checked get ALL
                if (node.IsChecked) CollectAllFiles(node, result);
                else result.AddRange(GetSelectedFiles(node.Children));
            }
            else if (node.IsChecked) result.Add(node.FullPath);
        }
    }
    
    private void CollectAllFiles(ArchiveNode node, List<string> list)
    {
        foreach (var child in node.Children)
        {
            if (child.IsDirectory) CollectAllFiles(child, list);
            else list.Add(child.FullPath);
        }
    }
    
    public async Task InstallFiles(string archivePath, IEnumerable<ArchiveNode> archiveItems)
    {
        using var archive = ArchiveFactory.Open(archivePath);
        Dictionary<string, IArchiveEntry> lookup = new Dictionary<string, IArchiveEntry>();

        foreach (var entry in archive.Entries)
        {
            if(entry.IsDirectory || string.IsNullOrEmpty(entry.Key)) continue;
            lookup[entry.Key] = entry;
        }

        var selectedFiles = GetSelectedFiles(archiveItems);
        
        if (selectedFiles.Count == 0)
        {
            await LogManager.Instance.Log("No items selected from archive.", LogManager.LogType.Warning);
            return;
        }
        
        var gameFolderPath = AppConfig.Instance.GameFolderPath;
        var options = new ExtractionOptions { Overwrite = true, ExtractFullPath = false }; 

        foreach (var filePath in selectedFiles)
        {
            if (!lookup.TryGetValue(filePath, out var entry))
            {   
                await LogManager.Instance.Log($"Entry not found! Filepath: {filePath}", LogManager.LogType.Warning);
                continue;
            }
            
            string destination = Path.Combine(gameFolderPath, "Data", filePath);
            string? directory = Path.GetDirectoryName(destination);

            // you have to check before writing ; acording to code abive, dir shouldn't be null
            Directory.CreateDirectory(directory);
            await entry.WriteToFileAsync(destination, options);
        }
        
        await LogManager.Instance.Log($"{selectedFiles.Count} files installed.");
    }

    public async Task<IArchive?> OpenArchive(string archivePath)
    {
        return ArchiveFactory.Open(archivePath);
    }

    public async Task TryGetFomodConfig(string archivePath)
    {
        using var archive = ArchiveFactory.Open(archivePath);
        //var configPath = 
        if (!archive.Entries.Select(x => x.Key).Contains("fomod/ModuleConfig.xml")) return;
    }
    
    // I'll rewrite everything later, for now just make this work
    public async Task FomodInstallFile()
    {
        
    }

    // for required files/folders
    public async Task FomodStraightInstall()
    {
        
    }

    // methods for autodetecting
    // btw, they shouldn't work on Windows since I use '/' there
    // I'll get this done one day :)
    
    /// <summary>
    /// Tries to find Skyrim's SE folder inside the Steam folder.
    /// </summary>
    /// <returns>True, if folder has been found and set. Otherwise, false.</returns>
    public async Task<bool> TryGetGameFolder()
    {
        string userFolder = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        string possiblePath = Path.Combine(userFolder, ".local/share/Steam/steamapps/common/Skyrim Special Edition");

        if (!Directory.Exists(possiblePath))
        {
            await LogManager.Instance.Log("Game directory not found automatically!", LogManager.LogType.Error);
            return false;
        }

        await AppConfig.Instance.UpdateConfig(AppConfig.ConfigType.GameFolderPath, possiblePath);
        return true;
    }

    /// <summary>
    /// Tries to find game's Plugin.txt file, located in compatdata folder.
    /// </summary>
    /// <returns>True, if file has been found and set. Otherwise, false.</returns>
    public async Task<bool> TryGetPluginConfig()
    {
        string userFolder = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        // ID 489830 is specifically for Skyrim Special Edition
        string possiblePath = Path.Combine(userFolder, ".local/share/Steam/steamapps/compatdata/489830/pfx/drive_c/users/steamuser/AppData/Local/Skyrim Special Edition/Plugins.txt");

        if (!File.Exists(possiblePath))
        {
            await LogManager.Instance.Log("Game config file not found automatically!", LogManager.LogType.Error);
            return false;
        }
        
        await AppConfig.Instance.UpdateConfig(AppConfig.ConfigType.PluginFilePath, possiblePath);
        return true;
    }

    /// <summary>
    /// Tries to find Steam's shortcuts.vdf file, needed for detecting the launcher ID
    /// </summary>
    /// <returns>True, if file has been found and set. Otherwise, false.</returns>
    public async Task<bool> TryGetVdfConfig()
    {
        //.local/share/Steam/userdata/392653044/config/shortcuts.vdf
        string userFolder = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        // ID 489830 is specifically for Skyrim Special Edition
        string userdataFolder = Path.Combine(userFolder, ".local/share/Steam/userdata");

        if (!Directory.Exists(userdataFolder))
        {
            await LogManager.Instance.Log(".local/share/Steam/userdata directory not found!", LogManager.LogType.Error);
            return false;
        }

        var dirs = Directory.GetDirectories(userdataFolder);

        if (dirs.Length == 0)
        {
            await LogManager.Instance.Log("No user found in userdata folder!", LogManager.LogType.Error);
            return false;
        }
        
        // we don't exactly know which of them
        if (dirs.Length > 1)
        {
            await LogManager.Instance.Log("More than one user found in userdata folder!", LogManager.LogType.Error);
            return false;
        }

        var userId = dirs.First();
        string possiblePath = Path.Combine(userdataFolder, userId, "config/shortcuts.vdf");
        
        if (!File.Exists(possiblePath))
        {
            await LogManager.Instance.Log("Game config file not found automatically!", LogManager.LogType.Error);
            return false;
        }
        
        await AppConfig.Instance.UpdateConfig(AppConfig.ConfigType.VdfConfigPath, possiblePath);
        return true;
    }
}