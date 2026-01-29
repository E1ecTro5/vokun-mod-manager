using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
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
    
    public ICommand SelectDirectoryCommand { get; }
    
    public MainWindowViewModel()
    {
        CurrentPath = "not selected";
        PathFiles = new ObservableCollection<PathFile>
        { 
            new PathFile("TEST", PathFile.FileType.Directory)
        };

        SelectDirectoryCommand = new AsyncRelayCommand(() => SelectDirectory());
    }

    private async Task SelectDirectory()
    {
        var fileM = new FileManager(AppManager.Instance.MainWindow);
        await fileM.SelectDirectory();
        this.CurrentPath = fileM.CurrentPath ??= "NULL";
        await ReadFiles(CurrentPath);
    }

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
}