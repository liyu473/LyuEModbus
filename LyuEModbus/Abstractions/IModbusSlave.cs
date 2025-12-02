using NModbus;

namespace LyuEModbus.Abstractions;

/// <summary>
/// Modbus 从站配置
/// </summary>
public class ModbusSlaveOptions
{
    /// <summary>
    /// 监听地址
    /// </summary>
    public string IpAddress { get; set; } = "0.0.0.0";
    
    /// <summary>
    /// 监听端口
    /// </summary>
    public int Port { get; set; } = 502;
    
    /// <summary>
    /// 从站ID
    /// </summary>
    public byte SlaveId { get; set; } = 1;
    
    /// <summary>
    /// 初始化保持寄存器数量
    /// </summary>
    public ushort InitHoldingRegisterCount { get; set; } = 100;
    
    /// <summary>
    /// 初始化线圈数量
    /// </summary>
    public ushort InitCoilCount { get; set; } = 100;
    
    /// <summary>
    /// 变化检测间隔（毫秒）
    /// </summary>
    public int ChangeDetectionInterval { get; set; } = 100;
}

/// <summary>
/// Modbus 从站接口
/// </summary>
public interface IModbusSlave : IModbusClient
{
    /// <summary>
    /// 是否正在运行
    /// </summary>
    bool IsRunning { get; }
    
    /// <summary>
    /// 从站数据存储
    /// </summary>
    ISlaveDataStore? DataStore { get; }
    
    /// <summary>
    /// 启动从站
    /// </summary>
    Task StartAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 停止从站
    /// </summary>
    void Stop();
    
    /// <summary>
    /// 设置线圈值
    /// </summary>
    void SetCoil(ushort address, bool value);
    
    /// <summary>
    /// 设置保持寄存器值
    /// </summary>
    void SetHoldingRegister(ushort address, ushort value);
    
    /// <summary>
    /// 批量设置保持寄存器
    /// </summary>
    void SetHoldingRegisters(ushort startAddress, ushort[] values);
    
    /// <summary>
    /// 读取保持寄存器值
    /// </summary>
    ushort[]? ReadHoldingRegisters(ushort startAddress, ushort count);
    
    /// <summary>
    /// 寄存器值被修改事件 (地址, 旧值, 新值)
    /// </summary>
    event Action<ushort, ushort, ushort>? HoldingRegisterWritten;
    
    /// <summary>
    /// 异步寄存器值被修改事件
    /// </summary>
    event Func<ushort, ushort, ushort, Task>? HoldingRegisterWrittenAsync;
    
    /// <summary>
    /// 线圈值被修改事件 (地址, 值)
    /// </summary>
    event Action<ushort, bool>? CoilWritten;
    
    /// <summary>
    /// 异步线圈值被修改事件
    /// </summary>
    event Func<ushort, bool, Task>? CoilWrittenAsync;
    
    /// <summary>
    /// 客户端连接事件 (客户端地址)
    /// </summary>
    event Action<string>? ClientConnected;
    
    /// <summary>
    /// 异步客户端连接事件
    /// </summary>
    event Func<string, Task>? ClientConnectedAsync;
    
    /// <summary>
    /// 客户端断开事件 (客户端地址)
    /// </summary>
    event Action<string>? ClientDisconnected;
    
    /// <summary>
    /// 异步客户端断开事件
    /// </summary>
    event Func<string, Task>? ClientDisconnectedAsync;
}
