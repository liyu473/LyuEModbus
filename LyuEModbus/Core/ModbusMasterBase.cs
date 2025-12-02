using LyuEModbus.Abstractions;
using LyuEModbus.Models;
using NModbus;
using static LyuEModbus.Models.ModbusConnectionState;

namespace LyuEModbus.Core;

/// <summary>
/// Modbus 主站抽象基类
/// </summary>
public abstract class ModbusMasterBase : IModbusMasterClient
{
    private ModbusConnectionState _state = Disconnected;
    
    protected readonly IModbusLogger Logger;
    protected NModbus.IModbusMaster? InternalMaster;
    
    public string ClientId { get; }
    public string Name { get; }
    public abstract string Address { get; }
    public byte SlaveId { get; protected set; }
    
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
    
    // IModbusMaster
    public IModbusTransport Transport => InternalMaster?.Transport ?? throw new InvalidOperationException("未连接");
    
    public event Action<ModbusConnectionState>? StateChanged;
    public event Action<int>? Reconnecting;
    public event Action? Heartbeat;
    
    protected ModbusMasterBase(string name, IModbusLogger logger)
    {
        ClientId = Guid.NewGuid().ToString("N")[..8];
        Name = name;
        Logger = logger;
    }
    
    public abstract Task ConnectAsync(CancellationToken cancellationToken = default);
    public abstract void Disconnect();
    public abstract void StopReconnect();
    
    protected void OnReconnecting(int attempt) => Reconnecting?.Invoke(attempt);
    protected void OnHeartbeat() => Heartbeat?.Invoke();
    
    #region IModbusMaster 实现
    
    public bool[] ReadCoils(byte slaveAddress, ushort startAddress, ushort numberOfPoints)
        => InternalMaster?.ReadCoils(slaveAddress, startAddress, numberOfPoints) ?? throw new InvalidOperationException("未连接");
    
    public Task<bool[]> ReadCoilsAsync(byte slaveAddress, ushort startAddress, ushort numberOfPoints)
        => InternalMaster?.ReadCoilsAsync(slaveAddress, startAddress, numberOfPoints) ?? throw new InvalidOperationException("未连接");
    
    public bool[] ReadInputs(byte slaveAddress, ushort startAddress, ushort numberOfPoints)
        => InternalMaster?.ReadInputs(slaveAddress, startAddress, numberOfPoints) ?? throw new InvalidOperationException("未连接");
    
    public Task<bool[]> ReadInputsAsync(byte slaveAddress, ushort startAddress, ushort numberOfPoints)
        => InternalMaster?.ReadInputsAsync(slaveAddress, startAddress, numberOfPoints) ?? throw new InvalidOperationException("未连接");
    
    public ushort[] ReadHoldingRegisters(byte slaveAddress, ushort startAddress, ushort numberOfPoints)
        => InternalMaster?.ReadHoldingRegisters(slaveAddress, startAddress, numberOfPoints) ?? throw new InvalidOperationException("未连接");
    
    public Task<ushort[]> ReadHoldingRegistersAsync(byte slaveAddress, ushort startAddress, ushort numberOfPoints)
        => InternalMaster?.ReadHoldingRegistersAsync(slaveAddress, startAddress, numberOfPoints) ?? throw new InvalidOperationException("未连接");
    
    public ushort[] ReadInputRegisters(byte slaveAddress, ushort startAddress, ushort numberOfPoints)
        => InternalMaster?.ReadInputRegisters(slaveAddress, startAddress, numberOfPoints) ?? throw new InvalidOperationException("未连接");
    
    public Task<ushort[]> ReadInputRegistersAsync(byte slaveAddress, ushort startAddress, ushort numberOfPoints)
        => InternalMaster?.ReadInputRegistersAsync(slaveAddress, startAddress, numberOfPoints) ?? throw new InvalidOperationException("未连接");
    
    public void WriteSingleCoil(byte slaveAddress, ushort coilAddress, bool value)
        => InternalMaster?.WriteSingleCoil(slaveAddress, coilAddress, value);
    
    public Task WriteSingleCoilAsync(byte slaveAddress, ushort coilAddress, bool value)
        => InternalMaster?.WriteSingleCoilAsync(slaveAddress, coilAddress, value) ?? Task.CompletedTask;
    
    public void WriteSingleRegister(byte slaveAddress, ushort registerAddress, ushort value)
        => InternalMaster?.WriteSingleRegister(slaveAddress, registerAddress, value);
    
    public Task WriteSingleRegisterAsync(byte slaveAddress, ushort registerAddress, ushort value)
        => InternalMaster?.WriteSingleRegisterAsync(slaveAddress, registerAddress, value) ?? Task.CompletedTask;
    
    public void WriteMultipleCoils(byte slaveAddress, ushort startAddress, bool[] data)
        => InternalMaster?.WriteMultipleCoils(slaveAddress, startAddress, data);
    
    public Task WriteMultipleCoilsAsync(byte slaveAddress, ushort startAddress, bool[] data)
        => InternalMaster?.WriteMultipleCoilsAsync(slaveAddress, startAddress, data) ?? Task.CompletedTask;
    
    public void WriteMultipleRegisters(byte slaveAddress, ushort startAddress, ushort[] data)
        => InternalMaster?.WriteMultipleRegisters(slaveAddress, startAddress, data);
    
    public Task WriteMultipleRegistersAsync(byte slaveAddress, ushort startAddress, ushort[] data)
        => InternalMaster?.WriteMultipleRegistersAsync(slaveAddress, startAddress, data) ?? Task.CompletedTask;
    
    public ushort[] ReadWriteMultipleRegisters(byte slaveAddress, ushort startReadAddress, ushort numberOfPointsToRead, ushort startWriteAddress, ushort[] writeData)
        => InternalMaster?.ReadWriteMultipleRegisters(slaveAddress, startReadAddress, numberOfPointsToRead, startWriteAddress, writeData) ?? throw new InvalidOperationException("未连接");
    
    public Task<ushort[]> ReadWriteMultipleRegistersAsync(byte slaveAddress, ushort startReadAddress, ushort numberOfPointsToRead, ushort startWriteAddress, ushort[] writeData)
        => InternalMaster?.ReadWriteMultipleRegistersAsync(slaveAddress, startReadAddress, numberOfPointsToRead, startWriteAddress, writeData) ?? throw new InvalidOperationException("未连接");
    
    public TResponse ExecuteCustomMessage<TResponse>(IModbusMessage request) where TResponse : IModbusMessage, new()
    {
        if (InternalMaster == null) throw new InvalidOperationException("未连接");
        return InternalMaster.ExecuteCustomMessage<TResponse>(request);
    }
    
    public void WriteFileRecord(byte slaveAddress, ushort fileNumber, ushort startingAddress, byte[] data)
        => InternalMaster?.WriteFileRecord(slaveAddress, fileNumber, startingAddress, data);
    
    #endregion
    
    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            StateChanged = null;
            Reconnecting = null;
            Heartbeat = null;
        }
    }
    
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}
