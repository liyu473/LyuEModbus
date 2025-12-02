using LyuEModbus.Abstractions;
using NModbus;

namespace LyuEModbus.Core;

/// <summary>
/// Modbus 从站抽象基类
/// </summary>
public abstract class ModbusSlaveBase : IModbusSlaveClient
{
    private bool _isRunning;
    
    protected readonly IModbusLogger Logger;
    protected NModbus.IModbusSlave? InternalSlave;
    
    public string ServerId { get; }
    public string Name { get; }
    public abstract string Address { get; }
    
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
    
    // IModbusSlave
    public byte UnitId { get; protected set; }
    public ISlaveDataStore DataStore => InternalSlave?.DataStore ?? throw new InvalidOperationException("从站未启动");
    
    public event Action<bool>? RunningChanged;
    public event Action<ushort, ushort, ushort>? HoldingRegisterWritten;
    public event Action<ushort, bool>? CoilWritten;
    public event Action<string>? ClientConnected;
    public event Action<string>? ClientDisconnected;
    
    protected ModbusSlaveBase(string name, byte unitId, IModbusLogger logger)
    {
        ServerId = Guid.NewGuid().ToString("N")[..8];
        Name = name;
        UnitId = unitId;
        Logger = logger;
    }
    
    public abstract Task StartAsync(CancellationToken cancellationToken = default);
    public abstract void Stop();
    public abstract void SetCoil(ushort address, bool value);
    public abstract void SetHoldingRegister(ushort address, ushort value);
    public abstract void SetHoldingRegisters(ushort startAddress, ushort[] values);
    public abstract ushort[]? ReadHoldingRegisters(ushort startAddress, ushort count);
    
    protected void OnHoldingRegisterWritten(ushort address, ushort oldValue, ushort newValue) 
        => HoldingRegisterWritten?.Invoke(address, oldValue, newValue);
    
    protected void OnCoilWritten(ushort address, bool value) 
        => CoilWritten?.Invoke(address, value);
    
    protected void OnClientConnected(string endpoint) 
        => ClientConnected?.Invoke(endpoint);
    
    protected void OnClientDisconnected(string endpoint) 
        => ClientDisconnected?.Invoke(endpoint);
    
    // IModbusSlave
    public IModbusMessage ApplyRequest(IModbusMessage request)
        => InternalSlave?.ApplyRequest(request) ?? throw new InvalidOperationException("从站未启动");
    
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
    
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}
