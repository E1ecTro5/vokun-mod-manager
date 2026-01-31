using System.IO;
using System.Threading.Tasks;

namespace VokunModManager.Misc;

public class LogManager
{
    private static LogManager _instance;
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
    
    private string logPath;
    public string[] Logs;

    public LogManager()
    {
        logPath = Path.Combine(AppConfig.Instance.BaseDirectory, "logs.txt");
    }

    public async Task InitLogs()
    {
        // file HAVE to be created ; no exceptions in Log() should be accepted
        if (!File.Exists(logPath))
        {
            await using (File.Create(logPath)) { }
        }

        await Log("LogManager Initialized!");
    }
    
    public async Task Log(string message, LogType logType = LogType.Info)
    {
        switch (logType)
        {
            case LogType.Info:
                await using (StreamWriter sr = new StreamWriter(logPath)) { await sr.WriteLineAsync($"[INFO]: {message}"); }
                break;
            case LogType.Warning:
                await using (StreamWriter sr = new StreamWriter(logPath)) { await sr.WriteLineAsync($"[WARN]: {message}"); }
                break;
            case LogType.Error:
                await using (StreamWriter sr = new StreamWriter(logPath)) { await sr.WriteLineAsync($"[ERR]: {message}"); }
                break;
        }
    }
}