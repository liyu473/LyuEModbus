namespace LyuEModbus.Abstractions;

/// <summary>
/// Modbus 服务端（从站）基础接口
/// </summary>
public interface IModbusServer : IDisposable
{
    /// <summary>
    /// 服务端唯一标识
    /// </summary>
    string ServerId { get; }
    
    /// <summary>
    /// 服务端名称
    /// </summary>
    string Name { get; }
    
    /// <summary>
    /// 监听地址（IP:Port）
    /// </summary>
    string Address { get; }
    
    /// <summary>
    /// 从站单元ID
    /// </summary>
    byte UnitId { get; }
    
    /// <summary>
    /// 是否正在运行
    /// </summary>
    bool IsRunning { get; }
    
    /// <summary>
    /// 运行状态变化事件
    /// </summary>
    event Action<bool>? RunningChanged;
}
