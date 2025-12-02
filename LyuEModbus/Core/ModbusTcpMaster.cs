using System.Net.Sockets;
using LyuEModbus.Abstractions;
using NModbus;

namespace LyuEModbus.Core;

/// <summary>
/// Modbus TCP 主站实现
/// </summary>
internal class ModbusTcpMaster : ModbusClientBase, IModbusMasterClient
{
    private TcpClient? _client;
    private NModbus.IModbusMaster? _master;
    private CancellationTokenSource? _reconnectCts;
    private CancellationTokenSource? _heartbeatCts;
    private bool _disposed;
    
    private readonly ModbusMasterOptions _options;
    
    public override string Address => $"{_options.IpAddress}:{_options.Port}";
    
    // IModbusMaster 实现
    public IModbusTransport Transport => _master?.Transport ?? throw new InvalidOperationException("未连接");
    
    public event Action<int>? Reconnecting;
    public event Action? Heartbeat;
    
    internal ModbusTcpMaster(string name, ModbusMasterOptions options, IModbusLogger logger) 
        : base(name, logger)
    {
        _options = options;
        SlaveId = options.SlaveId;
    }
    
    public async Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        if (IsConnected)
        {
            Logger.Log(LoggingLevel.Debug, "已连接，跳过");
            return;
        }
        await ConnectInternalAsync(cancellationToken);
    }
    
    private async Task ConnectInternalAsync(CancellationToken cancellationToken = default)
    {
        State = ModbusConnectionState.Connecting;
        
        try
        {
            _client = new TcpClient();
            await _client.ConnectAsync(_options.IpAddress, _options.Port, cancellationToken);
            
            _client.ReceiveTimeout = _options.ReadTimeout;
            _client.SendTimeout = _options.WriteTimeout;
            
            var factory = new ModbusFactory();
            _master = factory.CreateMaster(_client);
            _master.Transport.ReadTimeout = _options.ReadTimeout;
            _master.Transport.WriteTimeout = _options.WriteTimeout;
            
            State = ModbusConnectionState.Connected;
            Logger.Log(LoggingLevel.Information, $"已连接到 {Address}");
            
            if (_options.EnableHeartbeat)
                StartHeartbeat();
        }
        catch (Exception ex)
        {
            State = ModbusConnectionState.Disconnected;
            Logger.Log(LoggingLevel.Error, $"连接失败: {ex.Message}");
            throw;
        }
    }

    public void Disconnect()
    {
        StopReconnect();
        StopHeartbeat();
        
        if (!IsConnected && _client == null)
        {
            Logger.Log(LoggingLevel.Debug, "未连接");
            return;
        }
        
        try
        {
            _master?.Dispose();
            _client?.Close();
            _master = null;
            _client = null;
            State = ModbusConnectionState.Disconnected;
            Logger.Log(LoggingLevel.Information, "已断开连接");
        }
        catch (Exception ex)
        {
            Logger.Log(LoggingLevel.Error, $"断开失败: {ex.Message}");
            throw;
        }
    }
    
    public void StopReconnect()
    {
        _reconnectCts?.Cancel();
        _reconnectCts = null;
    }
    
    private void StartHeartbeat()
    {
        StopHeartbeat();
        _heartbeatCts = new CancellationTokenSource();
        
        _ = Task.Run(async () =>
        {
            while (_heartbeatCts != null && !_heartbeatCts.Token.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(_options.HeartbeatInterval, _heartbeatCts.Token);
                    Heartbeat?.Invoke();
                    
                    if (!CheckConnection())
                    {
                        Logger.Log(LoggingLevel.Warning, "心跳检测: 连接已断开");
                        await HandleConnectionLostAsync();
                        return;
                    }
                }
                catch (OperationCanceledException) { break; }
                catch (Exception ex)
                {
                    Logger.Log(LoggingLevel.Error, $"心跳异常: {ex.Message}");
                }
            }
        });
    }
    
    private void StopHeartbeat()
    {
        _heartbeatCts?.Cancel();
        _heartbeatCts = null;
    }
    
    private bool CheckConnection()
    {
        try
        {
            if (_client?.Client == null || !_client.Connected) return false;
            if (_client.Client.Poll(0, SelectMode.SelectRead))
            {
                byte[] buff = new byte[1];
                return _client.Client.Receive(buff, SocketFlags.Peek) != 0;
            }
            return true;
        }
        catch { return false; }
    }
    
    private async Task HandleConnectionLostAsync()
    {
        if (State == ModbusConnectionState.Reconnecting) return;
        StopHeartbeat();
        CleanupConnection();
        if (_options.AutoReconnect)
            await StartReconnectAsync();
    }
    
    private void CleanupConnection()
    {
        try { _master?.Dispose(); _client?.Close(); } catch { }
        _master = null;
        _client = null;
    }
    
    private async Task StartReconnectAsync()
    {
        if (!_options.AutoReconnect) return;
        if (_reconnectCts != null && !_reconnectCts.IsCancellationRequested) return;
        
        State = ModbusConnectionState.Reconnecting;
        _reconnectCts = new CancellationTokenSource();
        var attempts = 0;
        
        while (!_reconnectCts.Token.IsCancellationRequested)
        {
            attempts++;
            Reconnecting?.Invoke(attempts);
            Logger.Log(LoggingLevel.Information, $"重连 {attempts}/{(_options.MaxReconnectAttempts == 0 ? "∞" : _options.MaxReconnectAttempts.ToString())}");
            
            try
            {
                await ConnectInternalAsync(_reconnectCts.Token);
                return;
            }
            catch
            {
                if (_options.MaxReconnectAttempts > 0 && attempts >= _options.MaxReconnectAttempts)
                {
                    State = ModbusConnectionState.Disconnected;
                    return;
                }
                try { await Task.Delay(_options.ReconnectInterval, _reconnectCts.Token); }
                catch (OperationCanceledException) { break; }
            }
        }
        State = ModbusConnectionState.Disconnected;
    }
    
    private async Task<bool> EnsureConnectedAsync()
    {
        if (IsConnected && _master != null) return true;
        if (_options.AutoReconnect && State != ModbusConnectionState.Reconnecting)
        {
            CleanupConnection();
            await StartReconnectAsync();
            return IsConnected;
        }
        return false;
    }
    
    private async Task HandleCommunicationErrorAsync(Exception ex)
    {
        if (ex is IOException or SocketException)
        {
            Logger.Log(LoggingLevel.Error, $"通信异常: {ex.Message}");
            CleanupConnection();
            State = ModbusConnectionState.Disconnected;
            if (_options.AutoReconnect)
                _ = StartReconnectAsync();
        }
    }

    #region IModbusMaster 实现
    
    public bool[] ReadCoils(byte slaveAddress, ushort startAddress, ushort numberOfPoints)
        => _master?.ReadCoils(slaveAddress, startAddress, numberOfPoints) ?? throw new InvalidOperationException("未连接");
    
    public Task<bool[]> ReadCoilsAsync(byte slaveAddress, ushort startAddress, ushort numberOfPoints)
        => _master?.ReadCoilsAsync(slaveAddress, startAddress, numberOfPoints) ?? throw new InvalidOperationException("未连接");
    
    public bool[] ReadInputs(byte slaveAddress, ushort startAddress, ushort numberOfPoints)
        => _master?.ReadInputs(slaveAddress, startAddress, numberOfPoints) ?? throw new InvalidOperationException("未连接");
    
    public Task<bool[]> ReadInputsAsync(byte slaveAddress, ushort startAddress, ushort numberOfPoints)
        => _master?.ReadInputsAsync(slaveAddress, startAddress, numberOfPoints) ?? throw new InvalidOperationException("未连接");
    
    public ushort[] ReadHoldingRegisters(byte slaveAddress, ushort startAddress, ushort numberOfPoints)
        => _master?.ReadHoldingRegisters(slaveAddress, startAddress, numberOfPoints) ?? throw new InvalidOperationException("未连接");
    
    public Task<ushort[]> ReadHoldingRegistersAsync(byte slaveAddress, ushort startAddress, ushort numberOfPoints)
        => _master?.ReadHoldingRegistersAsync(slaveAddress, startAddress, numberOfPoints) ?? throw new InvalidOperationException("未连接");
    
    public ushort[] ReadInputRegisters(byte slaveAddress, ushort startAddress, ushort numberOfPoints)
        => _master?.ReadInputRegisters(slaveAddress, startAddress, numberOfPoints) ?? throw new InvalidOperationException("未连接");
    
    public Task<ushort[]> ReadInputRegistersAsync(byte slaveAddress, ushort startAddress, ushort numberOfPoints)
        => _master?.ReadInputRegistersAsync(slaveAddress, startAddress, numberOfPoints) ?? throw new InvalidOperationException("未连接");
    
    public void WriteSingleCoil(byte slaveAddress, ushort coilAddress, bool value)
        => _master?.WriteSingleCoil(slaveAddress, coilAddress, value);
    
    public Task WriteSingleCoilAsync(byte slaveAddress, ushort coilAddress, bool value)
        => _master?.WriteSingleCoilAsync(slaveAddress, coilAddress, value) ?? Task.CompletedTask;
    
    public void WriteSingleRegister(byte slaveAddress, ushort registerAddress, ushort value)
        => _master?.WriteSingleRegister(slaveAddress, registerAddress, value);
    
    public Task WriteSingleRegisterAsync(byte slaveAddress, ushort registerAddress, ushort value)
        => _master?.WriteSingleRegisterAsync(slaveAddress, registerAddress, value) ?? Task.CompletedTask;
    
    public void WriteMultipleCoils(byte slaveAddress, ushort startAddress, bool[] data)
        => _master?.WriteMultipleCoils(slaveAddress, startAddress, data);
    
    public Task WriteMultipleCoilsAsync(byte slaveAddress, ushort startAddress, bool[] data)
        => _master?.WriteMultipleCoilsAsync(slaveAddress, startAddress, data) ?? Task.CompletedTask;
    
    public void WriteMultipleRegisters(byte slaveAddress, ushort startAddress, ushort[] data)
        => _master?.WriteMultipleRegisters(slaveAddress, startAddress, data);
    
    public Task WriteMultipleRegistersAsync(byte slaveAddress, ushort startAddress, ushort[] data)
        => _master?.WriteMultipleRegistersAsync(slaveAddress, startAddress, data) ?? Task.CompletedTask;
    
    public ushort[] ReadWriteMultipleRegisters(byte slaveAddress, ushort startReadAddress, ushort numberOfPointsToRead, ushort startWriteAddress, ushort[] writeData)
        => _master?.ReadWriteMultipleRegisters(slaveAddress, startReadAddress, numberOfPointsToRead, startWriteAddress, writeData) ?? throw new InvalidOperationException("未连接");
    
    public Task<ushort[]> ReadWriteMultipleRegistersAsync(byte slaveAddress, ushort startReadAddress, ushort numberOfPointsToRead, ushort startWriteAddress, ushort[] writeData)
        => _master?.ReadWriteMultipleRegistersAsync(slaveAddress, startReadAddress, numberOfPointsToRead, startWriteAddress, writeData) ?? throw new InvalidOperationException("未连接");
    
    public TResponse ExecuteCustomMessage<TResponse>(NModbus.IModbusMessage request) where TResponse : NModbus.IModbusMessage, new()
    {
        if (_master == null) throw new InvalidOperationException("未连接");
        return _master.ExecuteCustomMessage<TResponse>(request);
    }
    
    public void WriteFileRecord(byte slaveAddress, ushort fileNumber, ushort startingAddress, byte[] data)
        => _master?.WriteFileRecord(slaveAddress, fileNumber, startingAddress, data);
    
    #endregion
    
    protected override void Dispose(bool disposing)
    {
        if (_disposed) return;
        _disposed = true;
        
        if (disposing)
        {
            StopReconnect();
            StopHeartbeat();
            CleanupConnection();
            Reconnecting = null;
            Heartbeat = null;
        }
        base.Dispose(disposing);
    }
}
