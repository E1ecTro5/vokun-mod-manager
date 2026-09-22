using System.Collections.ObjectModel;
using Avalonia.Threading;
using VokunModManager.Models;

namespace VokunModManager.Utils;

public class UiLoggerService : ILoggerService
{
    public ObservableCollection<LogMessage> Logs { get; } = new();
    private const int MaxLogNumber = 150; // I guess that's enough?
    // anyway, maybe we should add smth like FULL logs as a file one day?
    // right now, count of logs is stable. user won't just install MULTIPLE archives ONCE...

    public void Log(string message, LogLevel level = LogLevel.Info)
    {
        Dispatcher.UIThread.Post(() =>
        {
            Logs.Add(new LogMessage { Message = message, Level = level });
            
            // delete in ui thread
            while (Logs.Count > MaxLogNumber)
            {
                Logs.RemoveAt(0);
            }
        }, DispatcherPriority.Background);
    }

    public void Clear()
    {
        Dispatcher.UIThread.Post(() => Logs.Clear());
    }
}