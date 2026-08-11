using System;

namespace VokunModManager.Models;

public enum LogLevel
{
    Info,
    Success,
    Warning,
    Error
}

public class LogMessage
{
    public string Timestamp { get; } = DateTime.Now.ToString("HH:mm:ss.fff");
    public string Message { get; set; } = string.Empty;
    public LogLevel Level { get; set; } = LogLevel.Info;
}