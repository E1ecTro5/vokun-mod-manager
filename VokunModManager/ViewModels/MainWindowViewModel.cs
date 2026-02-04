using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VokunModManager.Misc;
using VokunModManager.Models;

namespace VokunModManager.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    [ObservableProperty] private ObservableCollection<Mod> _modList;
    
    [ObservableProperty] private string _gameFolderPath;     // Steam game folder
    [ObservableProperty] private string _pluginFilePath;     // plugins.txt file
    [ObservableProperty] private ulong _compatdataFolderId;  // compatdata ID for skse64_loader.exe
    [ObservableProperty] private ulong _modGameId;           // need for launching the skse64_loader.exe
    [ObservableProperty] private string _vdfFilePath;        // shortcuts.vdf file from Steam/userinfo/../config/..
    
    // these bad boys should be refactored ; their names doesn't match well with functions
    public ICommand SelectDirectoryCommand { get; }
    public ICommand SelectFileCommand { get; }
    public ICommand UpdateTextBlocksCommand { get; }
    public ICommand UpdateModListCommand { get; }
    public ICommand PlayClickCommand { get; }
    public ICommand SelectVdfCommand { get; }
    public ICommand SaveModListCommand { get; }
    
    public MainWindowViewModel()
    {
        ModList = new ObservableCollection<Mod>();

        SelectDirectoryCommand = new AsyncRelayCommand(SetGamePath);
        SelectFileCommand = new AsyncRelayCommand(SetModListPath);
        UpdateTextBlocksCommand = new AsyncRelayCommand(UpdateTextBlocks);
        UpdateModListCommand = new AsyncRelayCommand(UpdateModList);
        SelectVdfCommand = new AsyncRelayCommand(SelectVdf);

        PlayClickCommand = new AsyncRelayCommand(StartGame);
        SaveModListCommand = new AsyncRelayCommand(SaveModList);

        // ConfigFilePath = "AppConfig Path: " + AppConfig.Instance.BaseDirectory;
        GameFolderPath = AppConfig.Instance.GameFolderPath;
        PluginFilePath = AppConfig.Instance.PluginFilePath;
        ModGameId = AppConfig.Instance.GameId;
        VdfFilePath = AppConfig.Instance.VdfConfigPath;
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
        var modListM = new ModListManager(PluginFilePath);
        ModList = await modListM.GetModList();
        await LogManager.Instance.Log("Mod list updated.");
    }
    
    private async Task SaveModList()
    {
        await LogManager.Instance.Log("Saving current mod list state...");
        var modListM = new ModListManager(PluginFilePath);
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

    public async Task UpdateAll()
    {
        await UpdateTextBlocks();
        await UpdateModList();
    }
}