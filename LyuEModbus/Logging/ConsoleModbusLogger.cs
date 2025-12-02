using LyuEModbus.Abstractions;
using NModbus;

namespace LyuEModbus.Logging;

/// <summary>
/// 控制台日志记录器
/// </summary>
public class ConsoleModbusLogger : IModbusLogger
{
    private readonly string _name;
    
    public ConsoleModbusLogger(string name) => _name = name;
    
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
        Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] [{levelStr}] [{_name}] {message}");
    }
    
    public bool ShouldLog(LoggingLevel level) => true;
}
