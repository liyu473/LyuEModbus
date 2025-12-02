using System.ComponentModel;

namespace LyuEModbus.Models;

/// <summary>
/// Modbus 客户端连接状态
/// </summary>
public enum ModbusConnectionState
{
    /// <summary>
    /// 未连接
    /// </summary>
    [Description("未连接")]
    Disconnected,

    /// <summary>
    /// 连接中
    /// </summary>
    [Description("连接中")]
    Connecting,

    /// <summary>
    /// 已连接
    /// </summary>
    [Description("已连接")]
    Connected,

    /// <summary>
    /// 重连中
    /// </summary>
    [Description("重连中")]
    Reconnecting
}
