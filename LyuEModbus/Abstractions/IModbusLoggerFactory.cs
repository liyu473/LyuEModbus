namespace LyuEModbus.Abstractions;

/// <summary>
/// Modbus 日志工厂接口
/// </summary>
public interface IModbusLoggerFactory
{
    NModbus.IModbusLogger CreateLogger(string name);
}
