using System.IO;
using VokunModManager.ViewModels;
using VokunModManager.Views;

namespace VokunModManager;

public class AppManager
{
    private static AppManager? _instance;
    public static AppManager Instance
    {
        get
        {
            _instance ??= new();
            return _instance;
        }
    }

    private AppManager()
    {
        MainWindow = new MainWindow();
        MainWindowViewModel = new MainWindowViewModel();
    }
    
    public MainWindow MainWindow { get; }
    public MainWindowViewModel MainWindowViewModel { get; }
}