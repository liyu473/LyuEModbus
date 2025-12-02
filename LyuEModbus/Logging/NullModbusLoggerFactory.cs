using LyuEModbus.Abstractions;

namespace LyuEModbus.Logging;

/// <summary>
/// 空日志工厂
/// </summary>
public class NullModbusLoggerFactory : IModbusLoggerFactory
{
    public static readonly NullModbusLoggerFactory Instance = new();
    public NModbus.IModbusLogger CreateLogger(string name) => NullModbusLogger.Instance;
}
