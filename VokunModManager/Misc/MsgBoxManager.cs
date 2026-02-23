using System.Threading.Tasks;
using MsBox.Avalonia;

namespace VokunModManager.Misc;

public abstract class MsgBoxManager
{
    public static async Task ShowWarning(string message)
    {
        var messageBox = MessageBoxManager.GetMessageBoxStandard("Warning", message);
        await messageBox.ShowAsync();
    }
}