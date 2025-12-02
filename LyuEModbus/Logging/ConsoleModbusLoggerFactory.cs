using LyuEModbus.Abstractions;
using NModbus;

namespace LyuEModbus.Logging;

/// <summary>
/// 控制台日志工厂
/// </summary>
public class ConsoleModbusLoggerFactory : IModbusLoggerFactory
{
    public IModbusLogger CreateLogger(string name) => new ConsoleModbusLogger(name);
}
