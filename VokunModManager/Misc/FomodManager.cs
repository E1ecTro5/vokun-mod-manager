using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Xml.Linq;
using SharpCompress.Archives;
using SharpCompress.Readers;
using VokunModManager.Interfaces;
using VokunModManager.Models;
using VokunModManager.ViewModels;
using VokunModManager.Views;

namespace VokunModManager.Misc;

public class FomodManager(string archivePath)
{
    private string? _moduleName;
    private string? _defaultDestination;

    // cache
    private readonly HashSet<string> _createdDirectories = new(StringComparer.OrdinalIgnoreCase);

    public async Task InstallMod()
    {
        _defaultDestination = Path.Combine(AppConfig.Instance.GameFolderPath, "Data");
        
        // NormalizedKeyInArchive -> FullDestinationPathOnDisk
        var extractionMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        try
        {
            using (var archive = ArchiveFactory.Open(archivePath))
            {
                // caching entries
                var entries = archive.Entries.Where(x => !x.IsDirectory).ToList();

                if (await SearchForFomodFolder(entries))
                    //await InstallFromConfig(entries);
                    await PrepareFromConfig(entries, extractionMap);
                else
                    //await InstallWithoutConfig(entries);
                    PrepareWithoutConfig(entries, extractionMap);

                if (extractionMap.Count > 0) ExtractAllStreamlined(archive, extractionMap);

                entries.Clear();
            }
        }
        finally
        {
            extractionMap.Clear();
            _createdDirectories.Clear();

            // dispose and give the memory back to the OS
            GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
            GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true, compacting: true);
            GC.WaitForPendingFinalizers();
            GC.Collect();
            TrimProcessMemory();
        }
        
        _createdDirectories.Clear();

        // DISPOSE everything once finished
        GC.Collect();
        GC.WaitForPendingFinalizers();
    }
    
    private void ExtractAllStreamlined(IArchive archive, Dictionary<string, string> extractionMap)
    {
        // read the entire archive
        using var stream = File.OpenRead(archivePath);
        using var reader = ReaderFactory.Open(stream);

        while (reader.MoveToNextEntry())
        {
            if (reader.Entry.IsDirectory) continue;
            string normalizedKey = reader.Entry.Key.Replace('\\', '/');

            // chech if we need the file
            if (extractionMap.TryGetValue(normalizedKey, out string? destinationPath))
            {
                string? parentDir = Path.GetDirectoryName(destinationPath);
                
                if (!string.IsNullOrEmpty(parentDir) && _createdDirectories.Add(parentDir))
                {
                    Directory.CreateDirectory(parentDir);
                }
                
                using var fileStream = File.Create(destinationPath);
                reader.WriteEntryTo(fileStream);
            }
        }
    }

    private async Task<bool> SearchForFomodFolder(List<IArchiveEntry> entries)
    {
        var configEntry = entries.FirstOrDefault(x => x.Key != null && x.Key.EndsWith("ModuleConfig.xml", StringComparison.OrdinalIgnoreCase));
        if (configEntry?.Key == null) return false;

        // normalize the slashes
        string normalizedKey = configEntry.Key.Replace('\\', '/');
        _moduleName = normalizedKey.Split('/').FirstOrDefault() ?? "";
        
        return !string.IsNullOrEmpty(_moduleName);
    }
    
    private async Task PrepareFromConfig(List<IArchiveEntry> entries, Dictionary<string, string> extractionMap)
    {
        var fomodConfig = ReadConfig();
        
        foreach (var file in fomodConfig.RequiredFiles)
        {
            MapFile(file, entries, extractionMap);
        }
        
        foreach (var file in fomodConfig.RequiredFolders)
        {
            MapFile(file, entries, extractionMap);
        }

        IEnumerable<InstallStep> steps = fomodConfig.InstallSteps;
        foreach (var step in steps)
        {
            foreach (var fileGroup in step.Groups)
            {
                var type = fileGroup.Type;

                switch (type)
                {
                    case "SelectExactlyOne":
                        await ShowInstallWindow(fileGroup.Plugins, entries, extractionMap, InstallWindowViewModel.InstallType.SelectExactlyOne);
                        break;
                    case "SelectAny":
                        await ShowInstallWindow(fileGroup.Plugins, entries, extractionMap, InstallWindowViewModel.InstallType.SelectAny);
                        break;
                }
            }
        }
    }

    private async Task<List<PluginOption?>> ShowInstallWindow(IEnumerable<PluginOption> plugins, List<IArchiveEntry> entries, Dictionary<string, string> extractionMap, InstallWindowViewModel.InstallType type)
    {
        var window = new InstallWindow();
        var tcs = new TaskCompletionSource<List<PluginOption?>>();
        var vm = new InstallWindowViewModel(type, plugins, tcs, window);

        await vm.Init();
        window.DataContext = vm;
        window.Show();

        // waiting for result
        var result = await tcs.Task;
        
        foreach (var plugin in result.Where(p => p != null))
        {
            foreach (var file in plugin!.Files) MapFile(file, entries, extractionMap);
            foreach (var folder in plugin.Folders) MapFile(folder, entries, extractionMap);
        }
        
        return result;
    }

    private void MapFile(IMapping mapping, List<IArchiveEntry> entries, Dictionary<string, string> extractionMap)
    {
        string normModuleName = _moduleName?.Replace('\\', '/') ?? "";
        string normSource = mapping.Source.Replace('\\', '/').TrimStart('/');
        
        bool moduleIsTheParent = entries.All(x => x.Key.Replace('\\', '/').StartsWith(normModuleName, StringComparison.OrdinalIgnoreCase));
        string fullSource = moduleIsTheParent && !string.IsNullOrEmpty(normModuleName) ? $"{normModuleName}/{normSource}" : normSource;
        fullSource = fullSource.TrimEnd('/');
        string destinationFolder = Path.Combine(_defaultDestination!, mapping.Destination);

        foreach (var item in entries)
        {
            string itemKeyNormalized = item.Key.Replace('\\', '/');
            
            if (itemKeyNormalized.StartsWith(fullSource, StringComparison.OrdinalIgnoreCase))
            {
                string relativeTail = itemKeyNormalized.Substring(fullSource.Length).TrimStart('/');
                string fileDestination = Path.Combine(destinationFolder, relativeTail);
                
                // Запоминаем путь распаковки в карте
                extractionMap[itemKeyNormalized] = fileDestination;
            }
        }
    }
    
    private void PrepareWithoutConfig(List<IArchiveEntry> entries, Dictionary<string, string> extractionMap)
    {
        string destination = Path.Combine(AppConfig.Instance.GameFolderPath, "Data");
        int prefixSegmentsToSkip = DeterminePrefixSegmentsToSkip(entries);

        foreach (var entry in entries)
        {
            string normalizedKey = entry.Key.Replace('\\', '/');
            
            var segments = normalizedKey.Split('/');
            if (segments.Any(s => s.Equals("fomod", StringComparison.OrdinalIgnoreCase))) continue;

            int dataIndex = Array.FindIndex(segments, s => s.Equals("data", StringComparison.OrdinalIgnoreCase));
            
            string relativePath;
            
            if (dataIndex != -1) 
                relativePath = Path.Combine(segments.Skip(dataIndex + 1).ToArray());
            else if (prefixSegmentsToSkip > 0 && segments.Length > prefixSegmentsToSkip) 
                relativePath = Path.Combine(segments.Skip(prefixSegmentsToSkip).ToArray());
            else
                relativePath = Path.Combine(segments);

            string currentDestination = Path.Combine(destination, relativePath);

            extractionMap[normalizedKey] = currentDestination;
        }
    }

    private int DeterminePrefixSegmentsToSkip(IEnumerable<IArchiveEntry> entries)
    {
        // extensions and folders that identify the root of "Data" folder.
        var rootMarkers = new[] { ".bsa", ".ba2", ".esp", ".esm", ".esl" };
        var rootDirectories = new[] { "textures", "meshes", "interface", "sound", "music", "scripts", "skse" };

        int minSkip = int.MaxValue;
        
        foreach (var entry in entries)
        {
            string path = entry.Key.Replace('\\', '/');
            var parts = path.Split('/');

            for (int i = 0; i < parts.Length; i++)
            {
                string part = parts[i];

                bool isMarkerFile = rootMarkers.Any(ext => part.EndsWith(ext, StringComparison.OrdinalIgnoreCase));
                bool isMarkerDir = rootDirectories.Any(dir => part.Equals(dir, StringComparison.OrdinalIgnoreCase));

                if (isMarkerFile || isMarkerDir)
                {
                    // Запоминаем минимальную глубину, на которой встретили маркер
                    if (i < minSkip)
                    {
                        minSkip = i;
                    }
                    break; // Для этого файла дальше глубже не смотрим, берем наивысший совпавший маркер
                }
            }
        }

        return minSkip == int.MaxValue ? 0 : minSkip;; // Data is the root
    }

    /*
       So the structure of most of the configs should be like:
       <config>
           <moduleName>Some Mod</moduleName>
           <requiredInstallFiles>
               ...
           </requiredInstallFiles>
           <installSteps>
               <installStep>
                   <optionalFileGroups>
                       <group>
                           <plugins>
                               <plugin>
                                   <files>
                                       ...
                                   </files>
                               </plugin>
                           </plugins>
                       </group>
                   </optionalFileGroups>
               </installStep>
           </installSteps>
       </config>
     */
    
    // don't touch these methods until you know what you're doing
    
    private FomodConfig ReadConfig()
    {
        using var archive = ArchiveFactory.Open(archivePath);
        var entry = archive.Entries.FirstOrDefault(x => x.Key!.EndsWith("ModuleConfig.xml", StringComparison.OrdinalIgnoreCase));
        using var stream = entry!.OpenEntryStream();
        
        using var reader = new StreamReader(stream, detectEncodingFromByteOrderMarks: true);
        string text = reader.ReadToEnd();

        if (text.Contains('\0')) 
        {
            text = text.Replace("\0", "");
        }
        
        var doc = XDocument.Parse(text);
        var root = doc.Root;

        if (root == null) return null;

        FomodConfig config = new FomodConfig { ModuleName = root.Element("moduleName")?.Value };

        // ---------- REQUIRED FILES ----------
        var required = root.Element("requiredInstallFiles");
        if (required != null)
        {
            foreach (var file in required.Elements("file"))
            {
                config.RequiredFiles.Add(ParseFile(file));
            }

            foreach (var folder in required.Elements("folder"))
            {
                config.RequiredFolders.Add(ParseFolder(folder));
            }
        }

        // ---------- INSTALL STEPS ----------
        var steps = root.Element("installSteps");
        if (steps == null) return config;
        
        foreach (var stepElement in steps.Elements("installStep"))
        {
            var step = new InstallStep
            {
                Name = (string)stepElement.Attribute("name"),
                Groups = new List<FileGroup>()
            };

            var optionalGroups = stepElement.Element("optionalFileGroups");
            if (optionalGroups != null)
            {
                foreach (var groupElement in optionalGroups.Elements("group"))
                {
                    var group = new FileGroup
                    {
                        Name = (string)groupElement.Attribute("name"),
                        Type = (string)groupElement.Attribute("type"),
                        Plugins = new List<PluginOption>()
                    };

                    var pluginsElement = groupElement.Element("plugins");
                    if (pluginsElement != null)
                    {
                        foreach (var pluginElement in pluginsElement.Elements("plugin"))
                        {
                            var plugin = new PluginOption
                            {
                                Name = (string)pluginElement.Attribute("name"),
                                Description = pluginElement.Element("description")?.Value,
                                Files = new List<FileMapping>(),
                                Folders = new List<FolderMapping>()
                            };

                            var filesElement = pluginElement.Element("files");
                            if (filesElement != null)
                            {
                                foreach (var file in filesElement.Elements("file"))
                                {
                                    plugin.Files.Add(ParseFile(file));
                                }

                                foreach (var folder in filesElement.Elements("folder"))
                                {
                                    plugin.Folders.Add(ParseFolder(folder));
                                }
                            }

                            group.Plugins.Add(plugin);
                        }
                    }

                    step.Groups.Add(group);
                }
            }

            config.InstallSteps.Add(step);
        }

        return config;
    }
    
    private FileMapping ParseFile(XElement element)
    {
        return new FileMapping
        {
            Source = (string)element.Attribute("source"),
            Destination = (string)element.Attribute("destination"),
            Priority = (int?)element.Attribute("priority") ?? 0
        };
    }

    private FolderMapping ParseFolder(XElement element)
    {
        return new FolderMapping
        {
            Source = (string)element.Attribute("source"),
            Destination = (string)element.Attribute("destination"),
            Priority = (int?)element.Attribute("priority") ?? 0
        };
    }
    
    private static void TrimProcessMemory()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            try
            {
                using var process = Process.GetCurrentProcess();
                EmptyWorkingSet(process.Handle);
            }
            catch { /* Игнорируем на случай ограничений прав */ }
        }
    }

    [DllImport("psapi.dll")]
    private static extern bool EmptyWorkingSet(IntPtr hProcess);
}