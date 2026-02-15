using System;
using System.IO;
using System.Threading.Tasks;

namespace VokunModManager.Misc;

public class LogManager
{
    private static LogManager? _instance;
    public static LogManager Instance
    {
        get
        {
            _instance ??= new LogManager();
            return _instance;
        }
    }

    private readonly string _logPath = Path.Combine(AppConfig.Instance.BaseDirectory, "logs.txt");
    private StreamWriter? _logWriter;

    private LogManager()
    {
        if(!File.Exists(_logPath)) File.Create(_logPath);
        _logWriter = new StreamWriter(_logPath, true);
        _logWriter.WriteLine("LogManager Initialized!");
    }
    
    public enum LogType
    {
        Info,
        Warning,
        Error
    }
    
    public async Task Log(string message, LogType logType = LogType.Info)
    {
        TimeOnly time = TimeOnly.FromDateTime(DateTime.Now);
        string timeStr = $"{time.Hour}:{time.Minute}:{time.Second}.{time.Millisecond}";

        switch (logType)
        {
            case LogType.Info:
                await _logWriter.WriteLineAsync($"[{timeStr}][INFO]: {message}");
                break;
            case LogType.Warning:
                await _logWriter.WriteLineAsync($"[{timeStr}][WARN]: {message}");
                break;
            case LogType.Error:
                await _logWriter.WriteLineAsync($"[{timeStr}][ERR]: {message}");
                break;
        }

        // and don't forget to close
        // P.S.: maybe it's better to keep the stream open until the application closing

    }
}