using System.Collections.Generic;
using VokunModManager.Interfaces;

namespace VokunModManager.Models;

public class FomodConfig
{
    public string ModuleName { get; set; }

    public List<FileMapping> RequiredFiles { get; set; } = new();
    public List<FolderMapping> RequiredFolders { get; set; } = new();

    public List<InstallStep> InstallSteps { get; set; } = new();
}

public class InstallStep
{
    public string Name { get; set; }
    public List<FileGroup> Groups { get; set; }
}

public class FileGroup
{
    public string Name { get; set; }
    public string Type { get; set; }
    public List<PluginOption> Plugins { get; set; }
}

public class PluginOption
{
    public string Name { get; set; }
    public string Description { get; set; }

    public List<FolderMapping> Folders { get; set; }
    public List<FileMapping> Files { get; set; }
}

public class FolderMapping : IMapping
{
    public string Source { get; set; }
    public string Destination { get; set; }
    public int Priority { get; set; } = 0;
}

public class FileMapping : IMapping
{
    public string Source { get; set; }
    public string Destination { get; set; }
    public int Priority { get; set; } = 0;
}