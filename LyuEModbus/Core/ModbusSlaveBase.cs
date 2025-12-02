using LyuEModbus.Abstractions;
using NModbus;

namespace LyuEModbus.Core;

/// <summary>
/// Modbus 从站抽象基类
/// <para>提供 Modbus 从站（服务端）的基础实现，包括运行状态管理、数据存储访问和事件通知。</para>
/// <para>子类需要实现具体的网络监听逻辑（如 TCP、RTU 等）。</para>
/// </summary>
/// <remarks>
/// <para>主要功能：</para>
/// <list type="bullet">
///   <item>运行状态管理（IsRunning）</item>
///   <item>自动触发运行状态变化事件</item>
///   <item>代理 NModbus.IModbusSlave 的数据存储访问</item>
///   <item>提供寄存器/线圈修改、客户端连接/断开事件的触发机制</item>
/// </list>
/// <para>与主站的区别：</para>
/// <list type="bullet">
///   <item>主站主动连接从站，使用 IsConnected 表示连接状态</item>
///   <item>从站被动等待连接，使用 IsRunning 表示服务是否启动</item>
/// </list>
/// </remarks>
/// <param name="name">从站名称，用于日志和标识</param>
/// <param name="unitId">从站单元ID（Modbus 地址）</param>
/// <param name="logger">NModbus 日志记录器</param>
public abstract class ModbusSlaveBase(string name, byte unitId, IModbusLogger logger) : IModbusSlaveClient
{
    private bool _isRunning;
    
    /// <summary>
    /// 日志记录器，子类可用于记录日志
    /// </summary>
    protected readonly IModbusLogger Logger = logger;
    
    /// <summary>
    /// 内部 NModbus 从站实例，子类在启动成功后应设置此属性
    /// </summary>
    protected IModbusSlave? InternalSlave;

    /// <summary>
    /// 服务端唯一标识（8位随机字符串）
    /// </summary>
    public string ServerId { get; } = Guid.NewGuid().ToString("N")[..8];
    
    /// <summary>
    /// 从站名称
    /// </summary>
    public string Name { get; } = name;
    
    /// <summary>
    /// 监听地址，格式通常为 "IP:Port"，由子类实现
    /// </summary>
    public abstract string Address { get; }
    
    /// <summary>
    /// 是否正在运行
    /// <para>设置时会自动记录日志并触发 <see cref="RunningChanged"/> 事件</para>
    /// </summary>
    public bool IsRunning
    {
        get => _isRunning;
        protected set
        {
            if (_isRunning == value) return;
            _isRunning = value;
            Logger.Log(LoggingLevel.Information, $"运行状态: {(value ? "运行中" : "已停止")}");
            RunningChanged?.Invoke(value);
        }
    }

    /// <summary>
    /// 从站单元ID（Modbus 地址，1-247）
    /// </summary>
    public byte UnitId { get; protected set; } = unitId;
    
    /// <summary>
    /// 从站数据存储，包含线圈、离散输入、保持寄存器、输入寄存器
    /// </summary>
    /// <exception cref="InvalidOperationException">从站未启动时访问会抛出异常</exception>
    public ISlaveDataStore DataStore => InternalSlave?.DataStore ?? throw new InvalidOperationException("从站未启动");
    
    /// <summary>
    /// 运行状态变化事件
    /// </summary>
    public event Action<bool>? RunningChanged;
    
    /// <summary>
    /// 保持寄存器被主站修改事件
    /// <para>参数：(地址, 旧值, 新值)</para>
    /// </summary>
    public event Action<ushort, ushort, ushort>? HoldingRegisterWritten;
    
    /// <summary>
    /// 线圈被主站修改事件
    /// <para>参数：(地址, 新值)</para>
    /// </summary>
    public event Action<ushort, bool>? CoilWritten;
    
    /// <summary>
    /// 主站客户端连接事件
    /// <para>参数：客户端端点地址（如 "192.168.1.100:12345"）</para>
    /// </summary>
    public event Action<string>? ClientConnected;
    
    /// <summary>
    /// 主站客户端断开事件
    /// <para>参数：客户端端点地址</para>
    /// </summary>
    public event Action<string>? ClientDisconnected;

    /// <summary>
    /// 启动从站，开始监听连接
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    public abstract Task StartAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 停止从站
    /// </summary>
    public abstract void Stop();
    
    /// <summary>
    /// 设置单个线圈值
    /// </summary>
    /// <param name="address">线圈地址</param>
    /// <param name="value">线圈值</param>
    public abstract void SetCoil(ushort address, bool value);
    
    /// <summary>
    /// 设置单个保持寄存器值
    /// </summary>
    /// <param name="address">寄存器地址</param>
    /// <param name="value">寄存器值</param>
    public abstract void SetHoldingRegister(ushort address, ushort value);
    
    /// <summary>
    /// 批量设置保持寄存器值
    /// </summary>
    /// <param name="startAddress">起始地址</param>
    /// <param name="values">寄存器值数组</param>
    public abstract void SetHoldingRegisters(ushort startAddress, ushort[] values);
    
    /// <summary>
    /// 读取保持寄存器值
    /// </summary>
    /// <param name="startAddress">起始地址</param>
    /// <param name="count">读取数量</param>
    /// <returns>寄存器值数组，从站未启动时返回 null</returns>
    public abstract ushort[]? ReadHoldingRegisters(ushort startAddress, ushort count);
    
    /// <summary>
    /// 触发保持寄存器被修改事件（供子类调用）
    /// </summary>
    protected void OnHoldingRegisterWritten(ushort address, ushort oldValue, ushort newValue) 
        => HoldingRegisterWritten?.Invoke(address, oldValue, newValue);
    
    /// <summary>
    /// 触发线圈被修改事件（供子类调用）
    /// </summary>
    protected void OnCoilWritten(ushort address, bool value) 
        => CoilWritten?.Invoke(address, value);
    
    /// <summary>
    /// 触发客户端连接事件（供子类调用）
    /// </summary>
    protected void OnClientConnected(string endpoint) 
        => ClientConnected?.Invoke(endpoint);
    
    /// <summary>
    /// 触发客户端断开事件（供子类调用）
    /// </summary>
    protected void OnClientDisconnected(string endpoint) 
        => ClientDisconnected?.Invoke(endpoint);
    
    /// <summary>
    /// 应用 Modbus 请求（NModbus.IModbusSlave 接口实现）
    /// </summary>
    /// <param name="request">Modbus 请求消息</param>
    /// <returns>响应消息</returns>
    /// <exception cref="InvalidOperationException">从站未启动时抛出异常</exception>
    public IModbusMessage ApplyRequest(IModbusMessage request)
        => InternalSlave?.ApplyRequest(request) ?? throw new InvalidOperationException("从站未启动");
    
    /// <summary>
    /// 释放资源
    /// </summary>
    /// <param name="disposing">是否释放托管资源</param>
    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            RunningChanged = null;
            HoldingRegisterWritten = null;
            CoilWritten = null;
            ClientConnected = null;
            ClientDisconnected = null;
        }
    }
    
    /// <inheritdoc />
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}
