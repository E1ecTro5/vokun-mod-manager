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
            return  _instance;
        }
    }

    public enum LogType
    {
        Info,
        Warning,
        Error
    }
    
    private readonly string _logPath = Path.Combine(AppConfig.Instance.BaseDirectory, "logs.txt");

    public async Task InitLogs()
    {
        // file HAVE to be created ; no exceptions in Log() should be accepted
        // every session it has to be cleaned
        await using (File.Create(_logPath)) { }
        await using (StreamWriter sw = new StreamWriter(_logPath))
        {
            await sw.WriteLineAsync("LogManager Initialized!");
        }
    }
    
    public async Task Log(string message, LogType logType = LogType.Info)
    {
        // don't forget to APPEND
        StreamWriter sw = new StreamWriter(_logPath, true);
        TimeOnly time = TimeOnly.FromDateTime(DateTime.Now);
        // it won't give me second and millisec info, so I have to use this
        string  timeStr = $"{time.Hour}:{time.Minute}:{time.Second}.{time.Millisecond}";
        
        switch (logType)
        {
            case LogType.Info:
                await sw.WriteLineAsync($"[{timeStr}][INFO]: {message}");
                break;
            case LogType.Warning:
                await sw.WriteLineAsync($"[{timeStr}][WARN]: {message}");
                break;
            case LogType.Error:
                await sw.WriteLineAsync($"[{timeStr}][ERR]: {message}");
                break;
        }

        // and don't forget to close
        // P.S.: maybe it's better to keep the stream open until the application closing
        await sw.FlushAsync();
        sw.Close();
    }
}