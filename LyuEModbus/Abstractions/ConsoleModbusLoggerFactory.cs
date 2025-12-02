namespace LyuEModbus.Abstractions;

/// <summary>
/// 控制台日志工厂
/// </summary>
public class ConsoleModbusLoggerFactory : IModbusLoggerFactory
{
    public NModbus.IModbusLogger CreateLogger(string name) => new ConsoleModbusLogger(name);
}
