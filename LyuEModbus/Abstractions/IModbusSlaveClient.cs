using NModbus;

namespace LyuEModbus.Abstractions;

/// <summary>
/// Modbus 从站接口（继承 NModbus.IModbusSlave + IModbusServer）
/// </summary>
public interface IModbusSlaveClient : IModbusSlave, IModbusServer
{
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
    /// 寄存器被修改事件 (地址, 旧值, 新值)（支持异步回调）
    /// </summary>
    event Func<ushort, ushort, ushort, Task>? HoldingRegisterWritten;
    
    /// <summary>
    /// 线圈被修改事件 (地址, 值)（支持异步回调）
    /// </summary>
    event Func<ushort, bool, Task>? CoilWritten;
    
    /// <summary>
    /// 客户端连接事件（支持异步回调）
    /// </summary>
    event Func<string, Task>? ClientConnected;
    
    /// <summary>
    /// 客户端断开事件（支持异步回调）
    /// </summary>
    event Func<string, Task>? ClientDisconnected;
}
