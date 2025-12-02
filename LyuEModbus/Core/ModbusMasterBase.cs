using LyuEModbus.Abstractions;
using LyuEModbus.Models;
using NModbus;
using static LyuEModbus.Models.ModbusConnectionState;

namespace LyuEModbus.Core;

/// <summary>
/// Modbus 主站抽象基类
/// <para>提供 Modbus 主站的基础实现，包括连接管理、状态跟踪和 NModbus.IModbusMaster 接口的代理实现。</para>
/// <para>子类需要实现具体的连接逻辑（如 TCP、RTU 等）。</para>
/// </summary>
/// <remarks>
/// <para>主要功能：</para>
/// <list type="bullet">
///   <item>连接状态管理（Disconnected、Connecting、Connected、Reconnecting）</item>
///   <item>自动触发状态变化事件</item>
///   <item>代理 NModbus.IModbusMaster 的所有读写方法</item>
///   <item>提供重连和心跳事件的触发机制</item>
/// </list>
/// </remarks>
/// <param name="name">主站名称，用于日志和标识</param>
/// <param name="logger">NModbus 日志记录器</param>
public abstract class ModbusMasterBase(string name, IModbusLogger logger) : IModbusMasterClient
{
    private ModbusConnectionState _state = Disconnected;
    
    /// <summary>
    /// 日志记录器，子类可用于记录日志
    /// </summary>
    protected readonly IModbusLogger Logger = logger;
    
    /// <summary>
    /// 内部 NModbus 主站实例，子类在连接成功后应设置此属性
    /// </summary>
    protected IModbusMaster? InternalMaster;

    /// <summary>
    /// 客户端唯一标识（8位随机字符串）
    /// </summary>
    public string ClientId { get; } = Guid.NewGuid().ToString("N")[..8];
    
    /// <summary>
    /// 主站名称
    /// </summary>
    public string Name { get; } = name;
    
    /// <summary>
    /// 连接地址，格式通常为 "IP:Port"，由子类实现
    /// </summary>
    public abstract string Address { get; }
    
    /// <summary>
    /// 目标从站ID
    /// </summary>
    public byte SlaveId { get; protected set; }
    
    /// <summary>
    /// 当前连接状态
    /// <para>设置时会自动记录日志并触发 <see cref="StateChanged"/> 事件</para>
    /// </summary>
    public ModbusConnectionState State
    {
        get => _state;
        protected set
        {
            if (_state == value) return;
            _state = value;
            Logger.Log(LoggingLevel.Information, $"状态变更: {value}");
            StateChanged?.Invoke(value);
        }
    }
    
    /// <summary>
    /// 是否已连接（State == Connected）
    /// </summary>
    public bool IsConnected => State == Connected;
    
    /// <summary>
    /// NModbus 传输层，用于底层通信配置
    /// </summary>
    /// <exception cref="InvalidOperationException">未连接时访问会抛出异常</exception>
    public IModbusTransport Transport => InternalMaster?.Transport ?? throw new InvalidOperationException("未连接");
    
    /// <summary>
    /// 连接状态变化事件
    /// </summary>
    public event Action<ModbusConnectionState>? StateChanged;
    
    /// <summary>
    /// 重连事件，参数为（当前重连次数，最大重连次数）
    /// </summary>
    public event Action<int, int>? Reconnecting;
    
    /// <summary>
    /// 重连失败事件（达到最大重连次数后触发）
    /// </summary>
    public event Action? ReconnectFailed;
    
    /// <summary>
    /// 心跳事件，每次心跳检测时触发
    /// </summary>
    public event Action? Heartbeat;

    /// <summary>
    /// 连接到从站
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    public abstract Task ConnectAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 断开连接
    /// </summary>
    public abstract void Disconnect();
    
    /// <summary>
    /// 停止自动重连
    /// </summary>
    public abstract void StopReconnect();
    
    /// <summary>
    /// 触发重连事件（供子类调用）
    /// </summary>
    /// <param name="attempt">当前重连次数</param>
    /// <param name="maxAttempts">最大重连次数（0表示无限）</param>
    protected void OnReconnecting(int attempt, int maxAttempts) => Reconnecting?.Invoke(attempt, maxAttempts);
    
    /// <summary>
    /// 触发重连失败事件（供子类调用）
    /// </summary>
    protected void OnReconnectFailed() => ReconnectFailed?.Invoke();
    
    /// <summary>
    /// 触发心跳事件（供子类调用）
    /// </summary>
    protected void OnHeartbeat() => Heartbeat?.Invoke();

    #region IModbusMaster 实现 - 代理到 InternalMaster(NModbus原生对象)去实现

    /// <inheritdoc />
    public bool[] ReadCoils(byte slaveAddress, ushort startAddress, ushort numberOfPoints)
        => InternalMaster?.ReadCoils(slaveAddress, startAddress, numberOfPoints) ?? throw new InvalidOperationException("未连接");
    
    /// <inheritdoc />
    public Task<bool[]> ReadCoilsAsync(byte slaveAddress, ushort startAddress, ushort numberOfPoints)
        => InternalMaster?.ReadCoilsAsync(slaveAddress, startAddress, numberOfPoints) ?? throw new InvalidOperationException("未连接");
    
    /// <inheritdoc />
    public bool[] ReadInputs(byte slaveAddress, ushort startAddress, ushort numberOfPoints)
        => InternalMaster?.ReadInputs(slaveAddress, startAddress, numberOfPoints) ?? throw new InvalidOperationException("未连接");
    
    /// <inheritdoc />
    public Task<bool[]> ReadInputsAsync(byte slaveAddress, ushort startAddress, ushort numberOfPoints)
        => InternalMaster?.ReadInputsAsync(slaveAddress, startAddress, numberOfPoints) ?? throw new InvalidOperationException("未连接");
    
    /// <inheritdoc />
    public ushort[] ReadHoldingRegisters(byte slaveAddress, ushort startAddress, ushort numberOfPoints)
        => InternalMaster?.ReadHoldingRegisters(slaveAddress, startAddress, numberOfPoints) ?? throw new InvalidOperationException("未连接");
    
    /// <inheritdoc />
    public Task<ushort[]> ReadHoldingRegistersAsync(byte slaveAddress, ushort startAddress, ushort numberOfPoints)
        => InternalMaster?.ReadHoldingRegistersAsync(slaveAddress, startAddress, numberOfPoints) ?? throw new InvalidOperationException("未连接");
    
    /// <inheritdoc />
    public ushort[] ReadInputRegisters(byte slaveAddress, ushort startAddress, ushort numberOfPoints)
        => InternalMaster?.ReadInputRegisters(slaveAddress, startAddress, numberOfPoints) ?? throw new InvalidOperationException("未连接");
    
    /// <inheritdoc />
    public Task<ushort[]> ReadInputRegistersAsync(byte slaveAddress, ushort startAddress, ushort numberOfPoints)
        => InternalMaster?.ReadInputRegistersAsync(slaveAddress, startAddress, numberOfPoints) ?? throw new InvalidOperationException("未连接");
    
    /// <inheritdoc />
    public void WriteSingleCoil(byte slaveAddress, ushort coilAddress, bool value)
        => InternalMaster?.WriteSingleCoil(slaveAddress, coilAddress, value);
    
    /// <inheritdoc />
    public Task WriteSingleCoilAsync(byte slaveAddress, ushort coilAddress, bool value)
        => InternalMaster?.WriteSingleCoilAsync(slaveAddress, coilAddress, value) ?? Task.CompletedTask;
    
    /// <inheritdoc />
    public void WriteSingleRegister(byte slaveAddress, ushort registerAddress, ushort value)
        => InternalMaster?.WriteSingleRegister(slaveAddress, registerAddress, value);
    
    /// <inheritdoc />
    public Task WriteSingleRegisterAsync(byte slaveAddress, ushort registerAddress, ushort value)
        => InternalMaster?.WriteSingleRegisterAsync(slaveAddress, registerAddress, value) ?? Task.CompletedTask;
    
    /// <inheritdoc />
    public void WriteMultipleCoils(byte slaveAddress, ushort startAddress, bool[] data)
        => InternalMaster?.WriteMultipleCoils(slaveAddress, startAddress, data);
    
    /// <inheritdoc />
    public Task WriteMultipleCoilsAsync(byte slaveAddress, ushort startAddress, bool[] data)
        => InternalMaster?.WriteMultipleCoilsAsync(slaveAddress, startAddress, data) ?? Task.CompletedTask;
    
    /// <inheritdoc />
    public void WriteMultipleRegisters(byte slaveAddress, ushort startAddress, ushort[] data)
        => InternalMaster?.WriteMultipleRegisters(slaveAddress, startAddress, data);
    
    /// <inheritdoc />
    public Task WriteMultipleRegistersAsync(byte slaveAddress, ushort startAddress, ushort[] data)
        => InternalMaster?.WriteMultipleRegistersAsync(slaveAddress, startAddress, data) ?? Task.CompletedTask;
    
    /// <inheritdoc />
    public ushort[] ReadWriteMultipleRegisters(byte slaveAddress, ushort startReadAddress, ushort numberOfPointsToRead, ushort startWriteAddress, ushort[] writeData)
        => InternalMaster?.ReadWriteMultipleRegisters(slaveAddress, startReadAddress, numberOfPointsToRead, startWriteAddress, writeData) ?? throw new InvalidOperationException("未连接");
    
    /// <inheritdoc />
    public Task<ushort[]> ReadWriteMultipleRegistersAsync(byte slaveAddress, ushort startReadAddress, ushort numberOfPointsToRead, ushort startWriteAddress, ushort[] writeData)
        => InternalMaster?.ReadWriteMultipleRegistersAsync(slaveAddress, startReadAddress, numberOfPointsToRead, startWriteAddress, writeData) ?? throw new InvalidOperationException("未连接");
    
    /// <inheritdoc />
    public TResponse ExecuteCustomMessage<TResponse>(IModbusMessage request) where TResponse : IModbusMessage, new()
    {
        if (InternalMaster == null) throw new InvalidOperationException("未连接");
        return InternalMaster.ExecuteCustomMessage<TResponse>(request);
    }
    
    /// <inheritdoc />
    public void WriteFileRecord(byte slaveAddress, ushort fileNumber, ushort startingAddress, byte[] data)
        => InternalMaster?.WriteFileRecord(slaveAddress, fileNumber, startingAddress, data);
    
    #endregion
    
    /// <summary>
    /// 释放资源
    /// </summary>
    /// <param name="disposing">是否释放托管资源</param>
    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            StateChanged = null;
            Reconnecting = null;
            ReconnectFailed = null;
            Heartbeat = null;
        }
    }
    
    /// <inheritdoc />
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}
