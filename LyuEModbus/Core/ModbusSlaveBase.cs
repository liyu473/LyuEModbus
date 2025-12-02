using LyuEModbus.Abstractions;
using LyuEModbus.Models;
using NModbus;
using static LyuEModbus.Models.ModbusConnectionState;

namespace LyuEModbus.Core;

/// <summary>
/// Modbus 从站抽象基类
/// </summary>
public abstract class ModbusSlaveBase : IModbusSlaveClient
{
    private ModbusConnectionState _state = Disconnected;
    
    protected readonly IModbusLogger Logger;
    protected IModbusSlave? InternalSlave;
    
    public string ClientId { get; }
    public string Name { get; }
    public abstract string Address { get; }
    public byte SlaveId { get; protected set; }
    public virtual bool IsRunning { get; protected set; }
    
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
    
    public bool IsConnected => State == Connected;
    
    // IModbusSlave
    public byte UnitId => SlaveId;
    public ISlaveDataStore DataStore => InternalSlave?.DataStore ?? throw new InvalidOperationException("从站未启动");
    
    public event Action<ModbusConnectionState>? StateChanged;
    public event Action<ushort, ushort, ushort>? HoldingRegisterWritten;
    public event Action<ushort, bool>? CoilWritten;
    public event Action<string>? ClientConnected;
    public event Action<string>? ClientDisconnected;
    
    protected ModbusSlaveBase(string name, IModbusLogger logger)
    {
        ClientId = Guid.NewGuid().ToString("N")[..8];
        Name = name;
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
            StateChanged = null;
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
