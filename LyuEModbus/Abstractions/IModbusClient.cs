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
    /// 连接状态变化事件（支持异步回调）
    /// </summary>
    event Func<ModbusConnectionState, Task>? StateChanged;

    /// <summary>
    /// 记录日志
    /// </summary>
    /// <param name="level">日志级别</param>
    /// <param name="message">日志消息</param>
    void Log(ModbusLogLevel level, string message);
}
