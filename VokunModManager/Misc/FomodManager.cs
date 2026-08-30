using System.Diagnostics;
using System.Runtime;
using System.Runtime.InteropServices;
using System.Xml.Linq;
using SharpCompress.Archives;
using SharpCompress.Archives.SevenZip;
using SharpCompress.Readers;
using VokunModManager.Interfaces;
using VokunModManager.Models;
using VokunModManager.ViewModels;
using VokunModManager.Views;

namespace VokunModManager.Misc;

public class FomodManager(string archivePath, ILoggerService logger)
{
    private string? _moduleName;
    private string? _defaultDestination;

    // cache
    private readonly HashSet<string> _createdDirectories = new(StringComparer.OrdinalIgnoreCase);

    public async Task InstallMod()
    {
        _defaultDestination = Path.Combine(AppConfig.Instance.GameFolderPath, "Data");
        
        // open it ONCE
        using var archive = ArchiveFactory.Open(archivePath);
        
        // NormalizedKeyInArchive -> FullDestinationPathOnDisk
        var extractionMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        try
        {
            var (entries, hasFomod) = await Task.Run(() =>
            {
                logger.Log("Opening the archive...");
                var validEntries = archive.Entries.Where(x => !x.IsDirectory).ToList();
                logger.Log($"{validEntries.Count} entries found inside the archive.");

                bool fomodFound = SearchForFomodFolder(validEntries);
                return (validEntries, fomodFound);
            });

            if (hasFomod) await PrepareFromConfig(entries, extractionMap);
            else await Task.Run(() => PrepareWithoutConfig(entries, extractionMap));

            if (extractionMap.Count == 0)
            {
                logger.Log("Not a single fil selected in extractionMap. Make sure the config is not broken and you've chosen the options");
            }
            // extracting...
            else if (extractionMap.Count > 0)
            {
                logger.Log("Extracting files...");
                await Task.Run(() => { ExtractAllStreamlined(extractionMap); });
                logger.Log($"{extractionMap.Count} files have been extracted.");
            }
        }
        catch (TaskCanceledException)
        {
            logger.Log("Mod installment has been canceled.", LogLevel.Error);
            await MsgBoxManager.ShowWarning("Mod installment has been canceled.");
        }
        catch (Exception ex)
        {
            logger.Log("Error during mod installment.", LogLevel.Error);
            await MsgBoxManager.ShowWarning($"Error: {ex.Message}");
        }
        finally
        {
            logger.Log("Disposing memory...");
            extractionMap.Clear();
            _createdDirectories.Clear();

            // cleaning and disposing the memory
            await Task.Run(() =>
            {
                GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
                GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true, compacting: true);
                GC.WaitForPendingFinalizers();
                GC.Collect();
                TrimProcessMemory();
            });
        }
        
        logger.Log("Mod installment finished.");
    }

    private void ExtractAllStreamlined(Dictionary<string, string> extractionMap)
    {
        if (extractionMap.Count == 0) return;
        
        bool is7Zip = archivePath.EndsWith(".7z", StringComparison.OrdinalIgnoreCase);
        
        int leftToExtract = extractionMap.Count;
        
        if (is7Zip)
        {
            using var archive = SevenZipArchive.Open(archivePath);
            using var reader = archive.ExtractAllEntries();

            while (reader.MoveToNextEntry())
            {
                if (reader.Entry.IsDirectory || reader.Entry.Key == null) continue;

                string normalizedKey = reader.Entry.Key.Replace('\\', '/');

                if (extractionMap.TryGetValue(normalizedKey, out string? destinationPath))
                {
                    ExtractSingleEntry(reader, destinationPath);
                    leftToExtract -= 1;
                    if(leftToExtract == 0) break;
                }
            }
        }
        
        else
        {
            using var archive = ArchiveFactory.Open(archivePath);

            foreach (var entry in archive.Entries)
            {
                if (entry.IsDirectory || entry.Key == null) continue;

                string normalizedKey = entry.Key.Replace('\\', '/');

                if (extractionMap.TryGetValue(normalizedKey, out string? destinationPath))
                {
                    ExtractSingleEntry(entry, destinationPath);
                    leftToExtract -= 1;
                    if(leftToExtract == 0) break;
                }
            }
        }
    }

    private void ExtractSingleEntry(object entryOrReader, string destinationPath)
    {
        // on Linux will be '/', on Windows '\'
        string cleanPath = destinationPath.Replace('/', Path.DirectorySeparatorChar) .Replace('\\', Path.DirectorySeparatorChar);
        if (string.IsNullOrWhiteSpace(cleanPath)) return;

        // fix registers i.e. 'meshes' and 'Meshes' (IMPORTANT FOR LINUX)
        // will take the existing one, not creating other variations
        cleanPath = PathResolver.NormalizePathForOs(cleanPath);
        
        string? parentDir = Path.GetDirectoryName(cleanPath);
        
        if (!string.IsNullOrEmpty(parentDir) && _createdDirectories.Add(parentDir)) Directory.CreateDirectory(parentDir);

        using var fileStream = File.Create(cleanPath);

        if (entryOrReader is IReader reader) reader.WriteEntryTo(fileStream);
        else if (entryOrReader is IArchiveEntry entry) entry.WriteTo(fileStream);
    }

    private bool SearchForFomodFolder(List<IArchiveEntry> entries)
    {
        var configEntry = entries.FirstOrDefault(x => x.Key != null && x.Key.EndsWith("ModuleConfig.xml", StringComparison.OrdinalIgnoreCase));
        if (configEntry?.Key == null)
        {
            logger.Log("ModuleConfig.xml has not been found.");
            return false;
        }

        // normalize the slashes
        string normalizedKey = configEntry.Key.Replace('\\', '/');
        _moduleName = normalizedKey.Split('/').FirstOrDefault() ?? "";
        logger.Log($"ModuleConfig.xml has been found. Module name: {_moduleName}");
        
        return !string.IsNullOrEmpty(_moduleName);
    }
    
    private async Task PrepareFromConfig(List<IArchiveEntry> entries, Dictionary<string, string> extractionMap)
    {
        logger.Log("Reading the config...");
        var fomodConfig = ReadConfig(entries);
        if (fomodConfig is null) throw new Exception("Couldn't read the config.");
        logger.Log("Config has been read. Mapping files...");
        
        foreach (var file in fomodConfig.RequiredFiles) MapFile(file, entries, extractionMap);
        foreach (var file in fomodConfig.RequiredFolders) MapFile(file, entries, extractionMap);

        IEnumerable<InstallStep> steps = fomodConfig.InstallSteps;
        foreach (var step in steps)
        {
            // this works fine, don't touch it
            foreach (var fileGroup in step.Groups)
            {
                logger.Log($"InstallWindow appeared. Group: {fileGroup.Name}. Type: {fileGroup.Type}");
                await ShowInstallWindow(fileGroup, entries, extractionMap);
            }
        }
        logger.Log($"extractionMap has been prepared with {extractionMap.Count}.");
    }

    private async Task ShowInstallWindow(FileGroup group, List<IArchiveEntry> entries, Dictionary<string, string> extractionMap)
    {
        var tcs = new TaskCompletionSource<List<PluginOption?>>();
        
        var vm = new InstallWindowViewModel(group, tcs);
        var window = new InstallWindow { DataContext = vm };
        window.Show();

        List<PluginOption?> result;
        
        // wait till the result
        result = await tcs.Task;
        window.Close();

        // map the files
        foreach (var plugin in result.Where(p => p != null))
        {
            logger.Log($"Mapping {plugin!.Name} files...");
            foreach (var file in plugin.Files) MapFile(file, entries, extractionMap);
            foreach (var folder in plugin.Folders) MapFile(folder, entries, extractionMap);
        }
    }

    private void MapFile(IMapping mapping, List<IArchiveEntry> entries, Dictionary<string, string> extractionMap)
    {
        string normModuleName = _moduleName?.Replace('\\', '/').Trim('/') ?? "";
        string normSource = mapping.Source.Replace('\\', '/').Trim('/'); // Trimstart?
        
        bool moduleIsTheParent = entries.All(x => x.Key.Replace('\\', '/').StartsWith(normModuleName, StringComparison.OrdinalIgnoreCase));
        
        string fullSource = moduleIsTheParent && !string.IsNullOrEmpty(normModuleName) ? $"{normModuleName}/{normSource}" : normSource;
        fullSource = fullSource.Trim('/'); // TrimEnd?
        
        // base dir
        string destAttr = mapping.Destination.Replace('\\', '/').Trim('/');

        foreach (var item in entries)
        {
            if (item.IsDirectory || item.Key == null) continue; // ignore folders
            string itemKeyNormalized = item.Key.Replace('\\', '/');
            
            // exact match
            if (itemKeyNormalized.Equals(fullSource, StringComparison.OrdinalIgnoreCase))
            {
                string fileName = Path.GetFileName(itemKeyNormalized);
            
                // if destination in XML contains file
                // else defaultDestination + destination + fileName.
                string finalDestination;
                if (!string.IsNullOrEmpty(destAttr) && Path.HasExtension(destAttr)) finalDestination = Path.Combine(_defaultDestination!, destAttr);
                else finalDestination = Path.Combine(_defaultDestination!, destAttr, fileName);

                extractionMap[itemKeyNormalized] = finalDestination;
            }
            // XML points at a folder
            else if (itemKeyNormalized.StartsWith(fullSource + "/", StringComparison.OrdinalIgnoreCase))
            {
                string relativeTail = itemKeyNormalized.Substring(fullSource.Length).TrimStart('/');
                string finalDestination = Path.Combine(_defaultDestination!, destAttr, relativeTail);

                extractionMap[itemKeyNormalized] = finalDestination;
            }
        }
    }
    
    private void PrepareWithoutConfig(List<IArchiveEntry> entries, Dictionary<string, string> extractionMap)
    {
        string destination = Path.Combine(AppConfig.Instance.GameFolderPath, "Data");
        int prefixSegmentsToSkip = DeterminePrefixSegmentsToSkip(entries);

        logger.Log("Preparing without config...");
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
        logger.Log("extractionMap has been prepared.");
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
    
    private FomodConfig? ReadConfig(List<IArchiveEntry> archiveEntries)
    {
        // using var archive = ArchiveFactory.Open(archivePath);
        var entry = archiveEntries.FirstOrDefault(x => x.Key!.EndsWith("ModuleConfig.xml", StringComparison.OrdinalIgnoreCase));
        if (entry == null) return null;
        
        using var stream = entry.OpenEntryStream();
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
            catch
            {
                // ignore here
            }
        }
    }

    [DllImport("psapi.dll")]
    private static extern bool EmptyWorkingSet(IntPtr hProcess);
}