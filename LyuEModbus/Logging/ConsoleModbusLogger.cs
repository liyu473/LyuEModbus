using NModbus;

namespace LyuEModbus.Logging;

/// <summary>
/// 控制台日志记录器
/// </summary>
public class ConsoleModbusLogger(string name) : IModbusLogger
{
    public void Log(LoggingLevel level, string message)
    {
        var levelStr = level switch
        {
            LoggingLevel.Debug => "DBG",
            LoggingLevel.Information => "INF",
            LoggingLevel.Warning => "WRN",
            LoggingLevel.Error => "ERR",
            _ => "???"
        };
        Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] [{levelStr}] [{name}] {message}");
    }

    public bool ShouldLog(LoggingLevel level) => true;
}
