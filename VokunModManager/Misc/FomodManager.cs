using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using Avalonia.Controls;
using SharpCompress.Archives;
using SharpCompress.Common;
using SteamKit2.Internal;
using VokunModManager.Models;
using VokunModManager.ViewModels;
using VokunModManager.Views;

namespace VokunModManager.Misc;

public class FomodManager
{
    private string _archivePath;
    private string _fomodFilePath;
    
    private string _fomodConfigName;
    private const string FomodString = " FOMOD Installer"; // this is commonly used in most archives that have fomod ; NEED TO CHECK on other packages
    
    // make this ctor method soon
    public async Task SetArchive(string path)
    {
        _archivePath = path;
    }

    public async Task InstallMod()
    {
        /*
        
        if (!string.IsNullOrEmpty(_fomodFilePath))
        {
            // await SetArchive(); ? maybe ?
            await InstallFromConfig();
        }
        else await InstallWithoutConfig(); // just dump all to Data
        
        */
        
        // for test
        await InstallFromConfig();
    }
    
    private async Task InstallFromConfig()
    {
        var fomodConfig = ReadConfig();
        _fomodConfigName = fomodConfig.ModuleName;

        using var archive = ArchiveFactory.Open(_archivePath);
        var options = new ExtractionOptions { Overwrite = true, ExtractFullPath = false }; 
        
        var defaultDestination = Path.Combine(AppConfig.Instance.GameFolderPath, "Data");

        // check this one day, because I couldn't find archive with requredFiles
        // also maybe just put in method
        foreach (var file in fomodConfig.RequiredFiles)
        {
            var source = Path.Combine(string.Concat(fomodConfig.ModuleName, FomodString),file.Source);
            var destination = Path.Combine(defaultDestination, file.Destination);

            foreach (var item in archive.Entries.Where(x => !x.IsDirectory && x.Key.StartsWith(source)))
            {
                var lines = item.Key.Split(file.Source);
                var fileDestination = string.Concat(destination, lines[1]); // should always come after 'Required'
                var parentDir = Directory.GetParent(fileDestination).FullName;
                
                Directory.CreateDirectory(parentDir);
                await item.WriteToFileAsync(fileDestination, options);
            }
        }
        
        // this works good)
        foreach (var file in fomodConfig.RequiredFolders)
        {
            // extract FILES inside folder INTO DESTINATION
            var source = Path.Combine(string.Concat(fomodConfig.ModuleName, FomodString),file.Source);
            var destination = Path.Combine(defaultDestination, file.Destination);

            foreach (var item in archive.Entries.Where(x => !x.IsDirectory && x.Key.StartsWith(source)))
            {
                var lines = item.Key.Split(file.Source);
                var fileDestination = string.Concat(destination, lines[1]); // should always come after 'Required'
                var parentDir = Directory.GetParent(fileDestination).FullName;
                
                Directory.CreateDirectory(parentDir);
                await item.WriteToFileAsync(fileDestination, options);
            }
        }

        IEnumerable<InstallStep> steps = fomodConfig.InstallSteps; // these steps contains plugins with files/folders to install, so just make a separated method for installing
        // pass this to another installer with InstallWindow
        foreach (var file in steps)
        {
            foreach (var fileGroup in file.Groups)
            {
                var type = fileGroup.Type;

                switch (type)
                {
                    case "SelectExactlyOne":
                        await ShowSelectWindow(fileGroup.Plugins);
                        break;
                }
            }
        }
    }

    private async Task<PluginOption?> ShowSelectWindow(IEnumerable<PluginOption> plugins)
    {
        var window = new InstallWindow();
        var tcs = new TaskCompletionSource<PluginOption?>();
        
        var vm = new InstallWindowViewModel(window, plugins, tcs);
        window.DataContext = vm;

        window.Show();

        // waiting for result
        var result = await tcs.Task;
        
        if(result != null)
            await InstallSelectedPlugin(result);
        
        window.Close();
        return result;
    }

    private async Task InstallSelectedPlugin(PluginOption plugin)
    {
        var defaultDestination = Path.Combine(AppConfig.Instance.GameFolderPath, "Data");
        
        using var archive = ArchiveFactory.Open(_archivePath);
        var options = new ExtractionOptions { Overwrite = true, ExtractFullPath = false }; 
        
        foreach (var file in plugin.Files)
        {
            var source = Path.Combine(string.Concat(_fomodConfigName, FomodString),file.Source);
            var destination = Path.Combine(defaultDestination, file.Destination);

            foreach (var item in archive.Entries.Where(x => !x.IsDirectory && x.Key.StartsWith(source)))
            {
                var lines = item.Key.Split(file.Source);
                var fileDestination = string.Concat(destination, lines[1]); // should always come after 'Required'
                var parentDir = Directory.GetParent(fileDestination).FullName;
                
                Directory.CreateDirectory(parentDir);
                await item.WriteToFileAsync(fileDestination, options);
            }
        }
        
        foreach (var file in plugin.Folders)
        {
            var source = Path.Combine(string.Concat(_fomodConfigName, FomodString),file.Source);
            var destination = Path.Combine(defaultDestination, file.Destination);

            foreach (var item in archive.Entries.Where(x => !x.IsDirectory && x.Key.StartsWith(source)))
            {
                var lines = item.Key.Split(file.Source);
                var fileDestination = string.Concat(destination, lines[1]); // should always come after 'Required'
                var parentDir = Directory.GetParent(fileDestination).FullName;
                
                Directory.CreateDirectory(parentDir);
                await item.WriteToFileAsync(fileDestination, options);
            }
        }
    }

    // I'll handle it later
    private async Task InstallWithoutConfig()
    {
        return;
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