namespace LyuEModbus.Abstractions;

/// <summary>
/// Modbus 主站配置
/// </summary>
public class ModbusMasterOptions
{
    /// <summary>
    /// IP地址
    /// </summary>
    public string IpAddress { get; set; } = "127.0.0.1";
    
    /// <summary>
    /// 端口号
    /// </summary>
    public int Port { get; set; } = 502;
    
    /// <summary>
    /// 从站ID
    /// </summary>
    public byte SlaveId { get; set; } = 1;
    
    /// <summary>
    /// 读取超时（毫秒）
    /// </summary>
    public int ReadTimeout { get; set; } = 3000;
    
    /// <summary>
    /// 写入超时（毫秒）
    /// </summary>
    public int WriteTimeout { get; set; } = 3000;
    
    /// <summary>
    /// 是否启用自动重连
    /// </summary>
    public bool AutoReconnect { get; set; } = false;
    
    /// <summary>
    /// 重连间隔（毫秒）
    /// </summary>
    public int ReconnectInterval { get; set; } = 5000;
    
    /// <summary>
    /// 最大重连次数（0表示无限）
    /// </summary>
    public int MaxReconnectAttempts { get; set; } = 0;
    
    /// <summary>
    /// 是否启用心跳检测
    /// </summary>
    public bool EnableHeartbeat { get; set; } = false;
    
    /// <summary>
    /// 心跳间隔（毫秒）
    /// </summary>
    public int HeartbeatInterval { get; set; } = 5000;
}

/// <summary>
/// Modbus 主站接口
/// </summary>
public interface IModbusMaster : IModbusClient
{
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
    /// 重连事件（当前重连次数）
    /// </summary>
    event Action<int>? Reconnecting;
    
    /// <summary>
    /// 异步重连事件
    /// </summary>
    event Func<int, Task>? ReconnectingAsync;
    
    /// <summary>
    /// 心跳事件
    /// </summary>
    event Action? Heartbeat;
    
    /// <summary>
    /// 异步心跳事件
    /// </summary>
    event Func<Task>? HeartbeatAsync;
    
    #region 读取操作
    
    /// <summary>
    /// 读取线圈 (功能码 01)
    /// </summary>
    Task<bool[]> ReadCoilsAsync(ushort startAddress, ushort count, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 读取离散输入 (功能码 02)
    /// </summary>
    Task<bool[]> ReadDiscreteInputsAsync(ushort startAddress, ushort count, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 读取保持寄存器 (功能码 03)
    /// </summary>
    Task<ushort[]> ReadHoldingRegistersAsync(ushort startAddress, ushort count, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 读取输入寄存器 (功能码 04)
    /// </summary>
    Task<ushort[]> ReadInputRegistersAsync(ushort startAddress, ushort count, CancellationToken cancellationToken = default);
    
    #endregion
    
    #region 写入操作
    
    /// <summary>
    /// 写入单个线圈 (功能码 05)
    /// </summary>
    Task WriteSingleCoilAsync(ushort address, bool value, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 写入单个寄存器 (功能码 06)
    /// </summary>
    Task WriteSingleRegisterAsync(ushort address, ushort value, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 写入多个线圈 (功能码 15)
    /// </summary>
    Task WriteMultipleCoilsAsync(ushort startAddress, bool[] values, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 写入多个寄存器 (功能码 16)
    /// </summary>
    Task WriteMultipleRegistersAsync(ushort startAddress, ushort[] values, CancellationToken cancellationToken = default);
    
    #endregion
}
