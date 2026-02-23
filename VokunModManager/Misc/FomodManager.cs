using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using SharpCompress.Archives;
using SharpCompress.Common;
using VokunModManager.Interfaces;
using VokunModManager.Models;
using VokunModManager.ViewModels;
using VokunModManager.Views;

namespace VokunModManager.Misc;

public class FomodManager
{
    private string _archivePath;

    private string _moduleName;
    private string _fomodConfigName;
    private string _defaultDestination;
    
    // make this ctor method soon
    public async Task SetArchive(string path)
    {
        _archivePath = path;
    }

    public async Task InstallMod()
    {
        _defaultDestination = Path.Combine(AppConfig.Instance.GameFolderPath, "Data");
        using var archive = ArchiveFactory.Open(_archivePath);

        if (await SearchForFomodFolder(archive))
            await InstallFromConfig(archive);
        else
            await InstallWithoutConfig(archive);
    }
    
    private async Task InstallFromConfig(IArchive archive)
    {
        var fomodConfig = ReadConfig();
        await SearchForFomodFolder(archive);
        
        // check this one day, because I couldn't find archive with requiredFiles
        // also maybe just put in method
        foreach (var file in fomodConfig.RequiredFiles)
        {
            await HandleFile(file, archive);
        }
        
        // this works good)
        foreach (var file in fomodConfig.RequiredFolders)
        {
            // extract FILES inside folder INTO DESTINATION
            await HandleFile(file, archive);
        }

        IEnumerable<InstallStep> steps = fomodConfig.InstallSteps; // these steps contain plugins with files/folders to install, so just make a separated method for installing
        // pass this to another installer with InstallWindow
        foreach (var file in steps)
        {
            foreach (var fileGroup in file.Groups)
            {
                var type = fileGroup.Type;

                switch (type)
                {
                    case "SelectExactlyOne":
                        await ShowInstallWindow(fileGroup.Plugins, archive, InstallWindowViewModel.InstallType.SelectExactlyOne);
                        break;
                    case "SelectAny":
                        await ShowInstallWindow(fileGroup.Plugins, archive, InstallWindowViewModel.InstallType.SelectAny);
                        break;
                    // handle other types later
                }
            }
        }
    }

    private async Task<bool> SearchForFomodFolder(IArchive archive)
    {
        string? lines = archive.Entries.FirstOrDefault(x => (bool)x.Key?.EndsWith("ModuleConfig.xml"))?.Key?.Split(Path.DirectorySeparatorChar).FirstOrDefault();
        if (string.IsNullOrEmpty(lines)) return false;
        _moduleName = lines; // first is the head node
        return true;
    }

    private async Task<List<PluginOption?>> ShowInstallWindow(IEnumerable<PluginOption> plugins, IArchive archive, InstallWindowViewModel.InstallType type)
    {
        var window = new InstallWindow();
        var tcs = new TaskCompletionSource<List<PluginOption?>>();
        
        var vm = new InstallWindowViewModel(type, plugins, tcs, window);

        await vm.Init();
        
        window.DataContext = vm;
        window.Show();

        // waiting for result
        var result = await tcs.Task;
        
        if(result != null)
            await InstallSelectedPlugins(result, archive);
        
        window.Close();
        return result;
    }

    private async Task InstallSelectedPlugins(List<PluginOption> plugins, IArchive archive)
    {
        foreach (var plugin in plugins)
        {
            foreach (var file in plugin.Files)
            {
                await HandleFile(file, archive);
            }
            foreach (var file in plugin.Folders)
            {
                await HandleFile(file, archive);
            }
        }
    }

    private async Task HandleFile(IMapping mapping, IArchive archive)
    {
        var lines = archive.Entries;
        var moduleIsTheParent = lines.All(x => x.Key.StartsWith(_moduleName));
        var source = moduleIsTheParent ? Path.Combine(_moduleName, mapping.Source): mapping.Source;
        var destination = Path.Combine(_defaultDestination, mapping.Destination);

        foreach (var item in archive.Entries.Where(x => !x.IsDirectory && x.Key.StartsWith(source)))
        {
            var fileDestination = string.Concat(destination, item.Key.Split(mapping.Source)[1]); // should always come after 'Required'
            await Extract(item, fileDestination);
        }
    }
    
    private async Task Extract(IArchiveEntry item, string destination)
    {
        var parentDir = Directory.GetParent(destination).FullName;
        var options = new ExtractionOptions { Overwrite = true, ExtractFullPath = false }; 
        
        Directory.CreateDirectory(parentDir);
        await item.WriteToFileAsync(destination, options);
    }

    // I'll handle it later
    private async Task InstallWithoutConfig(IArchive archive)
    {
        //var isNodeTheParent = files.All(x => x.Key.StartsWith(files.FirstOrDefault().Key));
        // some mods have 'data' folder as the first node.
        //maybe you just need to Replace('data', string.Empty)?
        var destination = Path.Combine(AppConfig.Instance.GameFolderPath, "Data");
        
        // ignore fomod and data folders
        // also check out FNIS (or similar mods) which have fnis/Data/.. and some files alongside the data folder
        foreach (var entry in archive.Entries.Where(x => !x.IsDirectory))
        {
            var currentDestination = Path.Combine(destination, entry.Key);
            
            if(entry.Key.ToLower().Contains("fomod")) continue;
            if (entry.Key.ToLower().Contains("data"))
            {
                var currentKey = entry.Key.ToLower();
                var keyFolders = currentKey.Split(Path.DirectorySeparatorChar);
                var indexOfData = keyFolders.IndexOf("data");
                var newArr = entry.Key.Split(Path.DirectorySeparatorChar).Skip(indexOfData + 1);
                var newKey = string.Join(Path.DirectorySeparatorChar, newArr);
                currentDestination = Path.Combine(destination, newKey);
            }
            // don't forget to handle files that are at the same level as data folder
            
            await Extract(entry, currentDestination);
        }
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
        using var archive = ArchiveFactory.Open(_archivePath);
        var entry = archive.Entries.FirstOrDefault(x => x.Key.EndsWith("ModuleConfig.xml", StringComparison.OrdinalIgnoreCase));
        using var stream = entry.OpenEntryStream();
        
        var doc = XDocument.Load(stream);
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
}