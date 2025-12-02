using LyuEModbus.Core;
using NModbus;

namespace LyuEModbus.Abstractions;

/// <summary>
/// 扩展的 Modbus 主站接口（继承 NModbus.IModbusMaster + IModbusClient）
/// </summary>
public interface IModbusMasterClient : IModbusMaster, IModbusClient
{
    /// <summary>
    /// 轮询器（通过 WithPolling 配置后可用）
    /// </summary>
    ModbusPoller? Poller { get; }

    /// <summary>
    /// 轮询组（通过 WithPollerGroup 配置后可用）
    /// </summary>
    ModbusPollerGroup? PollerGroup { get; }

    /// <summary>
    /// 连接到从站
    /// </summary>
    Task ConnectAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 断开连接
    /// </summary>
    void Disconnect();
    
    /// <summary>
    /// 停止重连
    /// </summary>
    void StopReconnect();
    
    /// <summary>
    /// 重连事件，参数为（当前重连次数，最大重连次数）（支持异步回调）
    /// </summary>
    event Func<int, int, Task>? Reconnecting;
    
    /// <summary>
    /// 重连失败事件（达到最大重连次数后触发）（支持异步回调）
    /// </summary>
    event Func<Task>? ReconnectFailed;
    
    /// <summary>
    /// 心跳事件（支持异步回调）
    /// </summary>
    event Func<Task>? Heartbeat;
}
