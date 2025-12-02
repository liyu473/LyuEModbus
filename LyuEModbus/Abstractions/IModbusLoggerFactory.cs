using NModbus;

namespace LyuEModbus.Abstractions;

/// <summary>
/// Modbus 日志工厂接口
/// </summary>
public interface IModbusLoggerFactory
{
    IModbusLogger CreateLogger(string name);
}
