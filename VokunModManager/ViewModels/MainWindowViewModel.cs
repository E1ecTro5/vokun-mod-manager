using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VokunModManager.Misc;
using VokunModManager.Models;
using VokunModManager.Views;

namespace VokunModManager.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    [ObservableProperty] private ObservableCollection<PathFile> pathFiles;
    [ObservableProperty] private string currentPath;
    [ObservableProperty] private string? origGamePath;
    [ObservableProperty] private string configFilePath;
    [ObservableProperty] private string modListPath;
    
    public ICommand SelectDirectoryCommand { get; }
    public ICommand SetDirCommand { get; }
    public ICommand SetModPathCommand { get; }
    
    public MainWindowViewModel()
    {
        CurrentPath = "not selected";
        PathFiles = new ObservableCollection<PathFile>
        { 
            new PathFile("TEST", PathFile.FileType.Directory)
        };

        SelectDirectoryCommand = new AsyncRelayCommand(() => SelectDirectory());
        SetDirCommand = new AsyncRelayCommand(() => SelectPath("setGamePath"));
        SetModPathCommand = new AsyncRelayCommand(SelectModFile);

        ConfigFilePath = "AppConfig Path: " + AppConfig.Instance.BaseDirectory;
        ModListPath = "NONE";
    }

    // make it the only 
    private async Task SelectDirectory()
    {
        var fileM = new FileManager();
        this.CurrentPath = await fileM.SelectDirectory();
        await ReadFiles(CurrentPath);
    }

    // maybe this should be in FileManager, not here
    private async Task ReadFiles(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || path == "NULL") return;
        
        string[] entries = Directory.GetFileSystemEntries(path);
        var newList = new ObservableCollection<PathFile>();

        foreach (var fullPath in entries)
        {
            FileAttributes attr = File.GetAttributes(fullPath); // attributes of an object
            bool isDirectory = attr.HasFlag(FileAttributes.Directory); // check if it has Directory flag
            string name = Path.GetFileName(fullPath); // get only name of the file/dir
            PathFile.FileType fileType = isDirectory ? PathFile.FileType.Directory : PathFile.FileType.File;
            
            var file = new PathFile(name, fileType);
            newList.Add(file);
        }

        PathFiles = newList;
    }

    // refactor/remove
    private async Task SelectModFile()
    {
        var fileM = new FileManager();
        ModListPath = "ModList Path: " + await fileM.SelectFile();
        await SelectPath("setModPath");
    }
    
    // useless, remove this later
    private async Task SelectPath(string command)
    {
        string path = await new FileManager().SelectDirectory();

        switch (command)
        {
            case "setGamePath":
                await AppConfig.Instance.UpdateConfig("gamePath", path);
                break;
            case "setModPath":
                await AppConfig.Instance.UpdateConfig("modFilePath", path);
                break;
        }
    }
}