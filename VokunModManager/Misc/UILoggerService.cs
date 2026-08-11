using System.Collections.ObjectModel;
using Avalonia.Threading;
using VokunModManager.Models;

namespace VokunModManager.Misc;

public class UILoggerService : ILoggerService
{
    public ObservableCollection<LogMessage> Logs { get; } = new();

    public void Log(string message, LogLevel level = LogLevel.Info)
    {
        // using Dispatcher is necessary here
        Dispatcher.UIThread.Post(() =>
        {
            Logs.Add(new LogMessage { Message = message, Level = level });
        }, DispatcherPriority.Background);
    }

    public void Clear()
    {
        Dispatcher.UIThread.Post(() => Logs.Clear());
    }
}