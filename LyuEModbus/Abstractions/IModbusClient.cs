using LyuEModbus.Models;

namespace LyuEModbus.Abstractions;

/// <summary>
/// Modbus 客户端基础接口
/// </summary>
public interface IModbusClient : IDisposable
{
    /// <summary>
    /// 客户端唯一标识
    /// </summary>
    string ClientId { get; }
    
    /// <summary>
    /// 客户端名称
    /// </summary>
    string Name { get; }
    
    /// <summary>
    /// 连接地址（IP:Port）
    /// </summary>
    string Address { get; }
    
    /// <summary>
    /// 从站ID
    /// </summary>
    byte SlaveId { get; }
    
    /// <summary>
    /// 连接状态
    /// </summary>
    ModbusConnectionState State { get; }
    
    /// <summary>
    /// 是否已连接
    /// </summary>
    bool IsConnected { get; }
    
    /// <summary>
    /// 连接状态变化事件
    /// </summary>
    event Action<ModbusConnectionState>? StateChanged;
}
