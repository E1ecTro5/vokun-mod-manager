using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using MsBox.Avalonia;

namespace VokunModManager.Misc;

public abstract class MsgBoxManager
{
    public static async Task ShowWarning(string message)
    {
        var messageBox = MessageBoxManager.GetMessageBoxStandard("Warning", message);
        
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop && desktop.MainWindow != null)
            await messageBox.ShowWindowDialogAsync(desktop.MainWindow);
        else
            await messageBox.ShowAsync();
    }
}