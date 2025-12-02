using LyuEModbus.Abstractions;
using NModbus;

namespace LyuEModbus.Core;

/// <summary>
/// Modbus 客户端抽象基类
/// </summary>
public abstract class ModbusClientBase : IModbusClient
{
    private ModbusConnectionState _state = ModbusConnectionState.Disconnected;
    
    protected readonly IModbusLogger Logger;
    
    /// <inheritdoc />
    public string ClientId { get; }
    
    /// <inheritdoc />
    public string Name { get; }
    
    /// <inheritdoc />
    public abstract string Address { get; }
    
    /// <inheritdoc />
    public byte SlaveId { get; protected set; }
    
    /// <inheritdoc />
    public ModbusConnectionState State
    {
        get => _state;
        protected set
        {
            if (_state == value) return;
            _state = value;
            Logger.Log(LoggingLevel.Information, $"状态变更: {value}");
            StateChanged?.Invoke(value);
            StateChangedAsync?.Invoke(value);
        }
    }
    
    /// <inheritdoc />
    public bool IsConnected => State == ModbusConnectionState.Connected;
    
    /// <inheritdoc />
    public event Action<ModbusConnectionState>? StateChanged;
    
    /// <inheritdoc />
    public event Func<ModbusConnectionState, Task>? StateChangedAsync;
    
    protected ModbusClientBase(string name, IModbusLogger logger)
    {
        ClientId = Guid.NewGuid().ToString("N")[..8];
        Name = name;
        Logger = logger;
    }
    
    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            StateChanged = null;
            StateChangedAsync = null;
        }
    }
    
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}
