namespace LyuEModbus.Abstractions;

/// <summary>
/// Modbus 客户端连接状态
/// </summary>
public enum ModbusConnectionState
{
    Disconnected,
    Connecting,
    Connected,
    Reconnecting
}
