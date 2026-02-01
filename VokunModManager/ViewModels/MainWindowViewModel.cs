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
    [ObservableProperty] private ObservableCollection<Mod> modList;
    [ObservableProperty] private string currentPath;    // remove this later
    [ObservableProperty] private string configFilePath; // remove this later
    [ObservableProperty] private string origGamePath;  // Steam game folder
    [ObservableProperty] private string modListPath;    // plugins.txt file
    [ObservableProperty] private string modGameId;      // compatdata ID for skse64_loader.exe
    
    public ICommand SelectDirectoryCommand { get; }
    public ICommand SelectFileCommand { get; }
    public ICommand UpdateTextBlocksCommand { get; }
    public ICommand UpdateModListCommand { get; }
    
    public MainWindowViewModel()
    {
        CurrentPath = "not selected";
        ModList = new ObservableCollection<Mod>();

        SelectDirectoryCommand = new AsyncRelayCommand(SetGamePath);
        SelectFileCommand = new AsyncRelayCommand(SetModListPath);
        UpdateTextBlocksCommand = new AsyncRelayCommand(UpdateTextBlocks);
        UpdateModListCommand = new AsyncRelayCommand(UpdateModList);

        ConfigFilePath = "AppConfig Path: " + AppConfig.Instance.BaseDirectory;
        OrigGamePath = AppConfig.Instance.GameFolderPath;
        ModListPath = AppConfig.Instance.ModFilePath;
        ModGameId = AppConfig.Instance.ModGameSteamId.ToString();
    }

    private async Task UpdateTextBlocks()
    {
        OrigGamePath = AppConfig.Instance.GameFolderPath;
        ModListPath = AppConfig.Instance.ModFilePath;
        ModGameId = AppConfig.Instance.ModGameSteamId.ToString();
        await LogManager.Instance.Log("TextBlocks updated.");
    }

    private async Task UpdateModList()
    {
        await UpdateTextBlocks();
        await LogManager.Instance.Log("Updating mod list...");
        var modListM = new ModListManager(ModListPath);
        ModList = await modListM.GetModList();
    }

    private async Task SetGamePath()
    {
        var fileM = new FileManager();
        OrigGamePath = await fileM.SelectDirectory();
        // just make ENUM
        await AppConfig.Instance.UpdateConfig("gameFolderPath", OrigGamePath);
    }

    private async Task SetModListPath()
    {
        var fileM = new FileManager();
        ModListPath = await fileM.SelectFile();
        // just make ENUM
        await AppConfig.Instance.UpdateConfig("modFilePath", ModListPath);
    }

    // maybe this should be in FileManager, not here
    // this shi reads everything in the given path ; COMMENT/DOC LATER
    // P.S.: I deleted any ref with this Method, REPLACE IT LATER
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

        //PathFiles = newList;
    }
}