using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
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
    //[ObservableProperty] private ulong _compatdataFolderId;  // compatdata ID for skse64_loader.exe
    [ObservableProperty] private string _vdfFilePath;        // shortcuts.vdf file from Steam/userinfo/../config/..
    [ObservableProperty] private ulong _modGameId;           // need for launching the skse64_loader.exe
    
    [ObservableProperty] private string _archivePath;        // path of a selected archive

    [ObservableProperty] private bool _isPlayAvailable;
    [ObservableProperty] private bool _isLoadArchiveAvailable;

    private readonly FileManager _fileManager = new FileManager();

    /*
     Return this feature later
    [ObservableProperty] private float _installPercentage;
    [ObservableProperty] private float _maxPercentageValue;
    [ObservableProperty] private string _currentInstallFile;
    */
    
    // these bad boys should be refactored ; their names doesn't match well with functions
    public ICommand SelectDirectoryCommand { get; }
    public ICommand SelectFileCommand { get; }
    public ICommand UpdateTextBlocksCommand { get; }
    public ICommand UpdateModListCommand { get; }
    public ICommand PlayClickCommand { get; }
    public ICommand SelectVdfCommand { get; }
    public ICommand SelectLoaderCompatdataCommand { get; }
    public ICommand SaveModListCommand { get; }
    public ICommand LoadArchiveCommand { get; }
    public ICommand InstallModCommand { get; }
    
    public MainWindowViewModel()
    {
        ModList = new ObservableCollection<Mod>();

        SelectDirectoryCommand = new AsyncRelayCommand(SetGamePath);
        SelectFileCommand = new AsyncRelayCommand(SetModListPath);
        UpdateTextBlocksCommand = new AsyncRelayCommand(LateInit);  // possible rename because of refactor ; remind me later if needed
        UpdateModListCommand = new AsyncRelayCommand(UpdateModList);
        SelectVdfCommand = new AsyncRelayCommand(SelectVdf);
        SelectLoaderCompatdataCommand = new AsyncRelayCommand(SelectLoaderCompatdata);

        PlayClickCommand = new AsyncRelayCommand(StartGame);
        SaveModListCommand = new AsyncRelayCommand(SaveModList);
        LoadArchiveCommand = new AsyncRelayCommand(LoadArchive);

        InstallModCommand = new AsyncRelayCommand(InstallMod);
    }

    private async Task StartGame()
    {
        ulong longId = AppConfig.Instance.GameId;

        if (longId == 0)
        {
            // I should make something like MessageBox here...
            // I also added IsPlayAvailable, so I guess this will be removed soon
            await LogManager.Instance.Log("GameID has not been set!", LogManager.LogType.Error);
            return;
        }
        
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
    
    private async Task LateInit()
    {
        await LogManager.Instance.Log("Updating properties...");
        GameFolderPath = AppConfig.Instance.GameFolderPath;
        PluginFilePath = AppConfig.Instance.PluginFilePath;
        //CompatdataFolderId = AppConfig.Instance.CompatdataFolderId;
        ModGameId = AppConfig.Instance.GameId;
        VdfFilePath = AppConfig.Instance.VdfConfigPath;
        ArchivePath = "Not selected";
        IsPlayAvailable = AppConfig.Instance.GameId != 0;
        IsLoadArchiveAvailable = true;
        await LogManager.Instance.Log("Properties updated.");
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
        await UpdateModList();
        await LogManager.Instance.Log("Current mod list state saved...");
    }

    private async Task SelectVdf()
    {
        var filePath = await _fileManager.SelectFile();
        
        if (String.IsNullOrEmpty(filePath))
        {
            // no need to throw exception here
            await LogManager.Instance.Log("No file selected.");
            return;
        }

        VdfFilePath = filePath;
        await AppConfig.Instance.UpdateConfig(AppConfig.ConfigType.VdfConfigPath, filePath);
    }

    private async Task SelectLoaderCompatdata()
    {
        var compatdataDir = await _fileManager.SelectDirectory();
        
        if (String.IsNullOrEmpty(compatdataDir))
        {
            // no need to throw exception here
            await LogManager.Instance.Log("No directory selected.");
            return;
        }

        await AppConfig.Instance.UpdateConfig(AppConfig.ConfigType.CompatdataFolder, compatdataDir);
    }

    private async Task SetGamePath()
    {
        var filePath = await _fileManager.SelectFile();

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
        var filePath = await _fileManager.SelectFile();

        if (String.IsNullOrEmpty(filePath))
        {
            // no need to throw exception here
            await LogManager.Instance.Log("No file selected.");
            return;
        }

        PluginFilePath = filePath;
        await AppConfig.Instance.UpdateConfig(AppConfig.ConfigType.PluginFilePath, PluginFilePath);
    }

    private async Task InstallMod()
    {
        var fomod = new FomodManager();
        var filePath = await _fileManager.SelectFile();

        IsPlayAvailable = false;
        
        if(string.IsNullOrEmpty(filePath)) return;
        
        await fomod.SetArchive(filePath);
        await fomod.InstallMod();

        IsPlayAvailable = true;
        
        await UpdateModList();
    }

    private async Task LoadArchive()
    {
        var path =  await _fileManager.SelectFile();
        
        if(string.IsNullOrEmpty(path)) return;
        
        IsLoadArchiveAvailable = false;
        
        ArchivePath = path;
        ArchiveItems = await _fileManager.BuildTree(path);

        await LogManager.Instance.Log($"Archive {ArchivePath} has been loaded.");
        
        IsLoadArchiveAvailable = true;
    }

    public async Task UpdateAll()
    {
        await LateInit();
        await UpdateModList(); // maybe you should include this in LateInit()
    }
}