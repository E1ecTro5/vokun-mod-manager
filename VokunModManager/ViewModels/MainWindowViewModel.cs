using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SharpCompress.Archives;
using SharpCompress.Common;
using VokunModManager.Misc;
using VokunModManager.Models;

namespace VokunModManager.ViewModels;

// A LOT of things should be refactored here, I hope you won't forget

public partial class MainWindowViewModel : ViewModelBase
{
    [ObservableProperty] private ObservableCollection<Mod> _modList;
    [ObservableProperty] private ObservableCollection<Mod> _foundMods;
    [ObservableProperty] private ObservableCollection<ArchiveNode> _archiveItems; // items shown in specific border
    
    [ObservableProperty] private string _gameFolderPath;     // Steam game folder
    [ObservableProperty] private string _pluginFilePath;     // plugins.txt file
    [ObservableProperty] private ulong _compatdataFolderId;  // compatdata ID for skse64_loader.exe
    [ObservableProperty] private ulong _modGameId;           // need for launching the skse64_loader.exe
    [ObservableProperty] private string _vdfFilePath;        // shortcuts.vdf file from Steam/userinfo/../config/..
    [ObservableProperty] private string _archivePath;        // path of a selected archive

    [ObservableProperty] private float _installPercentage;
    [ObservableProperty] private float _maxPercentageValue;
    [ObservableProperty] private string _currentInstallFile;
    
    // these bad boys should be refactored ; their names doesn't match well with functions
    public ICommand SelectDirectoryCommand { get; }
    public ICommand SelectFileCommand { get; }
    public ICommand UpdateTextBlocksCommand { get; }
    public ICommand UpdateModListCommand { get; }
    public ICommand PlayClickCommand { get; }
    public ICommand SelectVdfCommand { get; }
    public ICommand SaveModListCommand { get; }
    public ICommand LoadArchiveCommand { get; }
    public ICommand InstallFilesCommand { get; }
    public ICommand IncludeModsCommand { get; }
    
    public MainWindowViewModel()
    {
        ModList = new ObservableCollection<Mod>();

        SelectDirectoryCommand = new AsyncRelayCommand(SetGamePath);
        SelectFileCommand = new AsyncRelayCommand(SetModListPath);
        UpdateTextBlocksCommand = new AsyncRelayCommand(UpdateTextBlocks);
        UpdateModListCommand = new AsyncRelayCommand(UpdateModList);
        SelectVdfCommand = new AsyncRelayCommand(SelectVdf);

        IncludeModsCommand = new AsyncRelayCommand(IncludeMods);

        PlayClickCommand = new AsyncRelayCommand(StartGame);
        SaveModListCommand = new AsyncRelayCommand(SaveModList);
        LoadArchiveCommand = new AsyncRelayCommand(LoadArchive);
        
        InstallFilesCommand = new AsyncRelayCommand(InstallFiles);

        // ConfigFilePath = "AppConfig Path: " + AppConfig.Instance.BaseDirectory;
        GameFolderPath = AppConfig.Instance.GameFolderPath;
        PluginFilePath = AppConfig.Instance.PluginFilePath;
        ModGameId = AppConfig.Instance.GameId;
        VdfFilePath = AppConfig.Instance.VdfConfigPath;

        ArchivePath = "Not selected";
    }

    private async Task StartGame()
    {
        ulong longId = AppConfig.Instance.GameId;
        
        // just uri command to run the game
        string uri = $"steam://rungameid/{longId}";
        
        await LogManager.Instance.Log($"Starting the game... ID:{longId}");
        
        // this variant should work on Linux;
        Process.Start(new ProcessStartInfo
        {
            FileName = uri,
            UseShellExecute = true,
            CreateNoWindow = true
        });
    }
    
    private async Task UpdateTextBlocks()
    {
        await LogManager.Instance.Log("Updating text blocks...");
        GameFolderPath = AppConfig.Instance.GameFolderPath;
        PluginFilePath = AppConfig.Instance.PluginFilePath;
        CompatdataFolderId = AppConfig.Instance.CompatdataFolderId;
        ModGameId = AppConfig.Instance.GameId;
        VdfFilePath = AppConfig.Instance.VdfConfigPath;
        await LogManager.Instance.Log("TextBlocks updated.");
    }

    private async Task UpdateModList()
    {
        await LogManager.Instance.Log("Updating mod list...");
        var modListM = new ModListManager();
        ModList = await modListM.GetModList();
        FoundMods = await modListM.CheckForMods(ModList);
        await LogManager.Instance.Log("Mod list updated.");
    }
    
    private async Task SaveModList()
    {
        await LogManager.Instance.Log("Saving current mod list state...");
        var modListM = new ModListManager();
        await modListM.SetModList(ModList);
        await LogManager.Instance.Log("Current mod list state saved...");
    }

    private async Task SelectVdf()
    {
        var fileM = new  FileManager();
        var filePath = await fileM.SelectFile();
        
        if (String.IsNullOrEmpty(filePath))
        {
            // no need to throw exception here
            await LogManager.Instance.Log("No file selected.");
            return;
        }

        VdfFilePath = filePath;
        await AppConfig.Instance.UpdateConfig(AppConfig.ConfigType.VdfConfigPath, filePath);
    }

    private async Task SetGamePath()
    {
        var fileM = new FileManager();
        var filePath = await fileM.SelectFile();

        if (String.IsNullOrEmpty(filePath))
        {
            // no need to throw exception here
            await LogManager.Instance.Log("No file selected.");
            return;
        }
        
        GameFolderPath = filePath;
        await AppConfig.Instance.UpdateConfig(AppConfig.ConfigType.GameFolderPath, this.GameFolderPath);
    }

    private async Task SetModListPath()
    {
        var fileM = new FileManager();
        var filePath = await fileM.SelectFile();

        if (String.IsNullOrEmpty(filePath))
        {
            // no need to throw exception here
            await LogManager.Instance.Log("No file selected.");
            return;
        }

        PluginFilePath = filePath;
        await AppConfig.Instance.UpdateConfig(AppConfig.ConfigType.PluginFilePath, PluginFilePath);
    }

    private async Task IncludeMods()
    {
        var items = FoundMods.Where(x => x.IsEnabled);
        var modM = new ModListManager();
        await modM.EnableMods(items);
        await UpdateModList();
    }

    private async Task LoadArchive()
    {
        var fileM = new FileManager();
        var path =  await fileM.SelectFile();
        
        if(string.IsNullOrEmpty(path)) return;
        
        ArchivePath = path;
        ArchiveItems = await fileM.BuildTree(path);
    }

    // update logs later
    private async Task InstallFiles()
    {
        await LogManager.Instance.Log("Installing files...");
        InstallPercentage = 0;
        var fileM = new FileManager();
        await InstallFiles(ArchivePath);
        await LogManager.Instance.Log("Files installed.");
    }
    
    private async Task InstallFiles(string archivePath)
    {
        using var archive = ArchiveFactory.Open(archivePath);

        var entryLookup = archive.Entries
            .Where(e => !e.IsDirectory && e.Key != null)
            .ToDictionary(e => e.Key!);

        var selectedFiles = new FileManager().GetSelectedFiles(ArchiveItems);
        float current = 0;
        MaxPercentageValue = selectedFiles.Count;

        foreach (var filePath in selectedFiles)
        {
            if (!entryLookup.TryGetValue(filePath, out var entry)) continue;
            
            string destination = Path.Combine(GameFolderPath, "Data", filePath);
            string? directory = Path.GetDirectoryName(destination);
            
            CurrentInstallFile = filePath;
            await LogManager.Instance.Log($"Writing {filePath} to {directory}");
            // you have to check before writing
            if (!Directory.Exists(directory)) Directory.CreateDirectory(directory);

            await entry.WriteToFileAsync(destination, new ExtractionOptions { Overwrite = true, ExtractFullPath = false });
            
            current++;
            InstallPercentage = current;
        }
    }


    public async Task UpdateAll()
    {
        await UpdateTextBlocks();
        await UpdateModList();
    }
}