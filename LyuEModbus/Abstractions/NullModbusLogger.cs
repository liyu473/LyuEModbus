using NModbus;

namespace LyuEModbus.Abstractions;

/// <summary>
/// 空日志记录器
/// </summary>
public class NullModbusLogger : IModbusLogger
{
    public static readonly NullModbusLogger Instance = new();
    public void Log(LoggingLevel level, string message) { }
    public bool ShouldLog(LoggingLevel level) => false;
}
