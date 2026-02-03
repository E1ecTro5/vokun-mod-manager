using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VokunModManager.Misc;
using VokunModManager.Models;

namespace VokunModManager.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    [ObservableProperty] private ObservableCollection<Mod> modList;
    [ObservableProperty] private string configFilePath; // remove this later
    [ObservableProperty] private string origGamePath;  // Steam game folder
    [ObservableProperty] private string modListPath;    // plugins.txt file
    [ObservableProperty] private string modGameId;      // compatdata ID for skse64_loader.exe
    
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

        ConfigFilePath = "AppConfig Path: " + AppConfig.Instance.BaseDirectory;
        OrigGamePath = AppConfig.Instance.GameFolderPath;
        ModListPath = AppConfig.Instance.ModFilePath;
        ModGameId = AppConfig.Instance.ModGameSteamId.ToString();
    }

    private async Task StartGame()
    {
        string exePath = Path.Combine(OrigGamePath, "skse64_loader.exe");
        ulong longId = GameIdFinder.GetLongId(exePath);
        
        // just uri command to run the game
        string uri = $"steam://rungameid/{longId}";

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
    
    private async Task SaveModList()
    {
        await LogManager.Instance.Log("Saving current mod list state...");
        var modListM = new ModListManager(ModListPath);
        await modListM.SetModList(ModList);
        await LogManager.Instance.Log("Current mod list state saved...");
    }

    private async Task SelectVdf()
    {
        var fileM = new  FileManager();
        var filePath = await fileM.SelectFile();
        await AppConfig.Instance.UpdateConfig("vdfConfigPath",  filePath);
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
}