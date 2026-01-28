using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
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
            new PathFile() { FileName = "TESTING" }
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
        if(path == "NULL") return;
        
        string[] dirs = Directory.GetDirectories(path);
        var newList = new ObservableCollection<PathFile>();
        
        foreach (var dir in dirs)
        {
            var file = new PathFile() { FileName = dir };
            newList.Add(file);
        }
        
        PathFiles =  newList;
    }
}