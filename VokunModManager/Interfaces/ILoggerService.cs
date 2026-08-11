using System.Collections.ObjectModel;
using VokunModManager.Models;

public interface ILoggerService
{
    ObservableCollection<LogMessage> Logs { get; }
    void Log(string message, LogLevel level = LogLevel.Info);
    void Clear();
}