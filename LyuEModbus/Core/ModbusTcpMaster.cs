using System.Net.Sockets;
using LyuEModbus.Abstractions;
using NModbus;

namespace LyuEModbus.Core;

/// <summary>
/// Modbus TCP 主站实现
/// </summary>
internal class ModbusTcpMaster : ModbusClientBase, Abstractions.IModbusMaster
{
    private TcpClient? _client;
    private NModbus.IModbusMaster? _master;
    private CancellationTokenSource? _reconnectCts;
    private CancellationTokenSource? _heartbeatCts;
    private bool _disposed;
    
    private readonly ModbusMasterOptions _options;
    
    public override string Address => $"{_options.IpAddress}:{_options.Port}";
    
    /// <inheritdoc />
    public event Action<int>? Reconnecting;
    
    /// <inheritdoc />
    public event Func<int, Task>? ReconnectingAsync;
    
    /// <inheritdoc />
    public event Action? Heartbeat;
    
    /// <inheritdoc />
    public event Func<Task>? HeartbeatAsync;
    
    internal ModbusTcpMaster(string name, ModbusMasterOptions options, IModbusLogger logger) 
        : base(name, logger)
    {
        _options = options;
        SlaveId = options.SlaveId;
    }
    
    /// <inheritdoc />
    public async Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        if (IsConnected)
        {
            Logger.Log(LoggingLevel.Debug, "已连接，跳过连接");
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
            {
                StartHeartbeat();
            }
        }
        catch (Exception ex)
        {
            State = ModbusConnectionState.Disconnected;
            Logger.Log(LoggingLevel.Error, $"连接失败: {ex.Message}");
            throw;
        }
    }

    /// <inheritdoc />
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
    
    /// <inheritdoc />
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
            Logger.Log(LoggingLevel.Debug, "心跳检测已启动");
            while (_heartbeatCts != null && !_heartbeatCts.Token.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(_options.HeartbeatInterval, _heartbeatCts.Token);
                    
                    Heartbeat?.Invoke();
                    if (HeartbeatAsync != null)
                        await HeartbeatAsync.Invoke();
                    
                    if (!CheckConnection())
                    {
                        Logger.Log(LoggingLevel.Warning, "心跳检测: 连接已断开");
                        await HandleConnectionLostAsync();
                        return;
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    Logger.Log(LoggingLevel.Error, $"心跳检测异常: {ex.Message}");
                }
            }
            Logger.Log(LoggingLevel.Debug, "心跳检测已停止");
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
        catch
        {
            return false;
        }
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
        try
        {
            _master?.Dispose();
            _client?.Close();
        }
        catch { }
        
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
        
        Logger.Log(LoggingLevel.Information, "开始自动重连...");
        
        while (!_reconnectCts.Token.IsCancellationRequested)
        {
            attempts++;
            Reconnecting?.Invoke(attempts);
            if (ReconnectingAsync != null)
                await ReconnectingAsync.Invoke(attempts);
            
            var maxStr = _options.MaxReconnectAttempts == 0 ? "∞" : _options.MaxReconnectAttempts.ToString();
            Logger.Log(LoggingLevel.Information, $"重连尝试 {attempts}/{maxStr}");
            
            try
            {
                await ConnectInternalAsync(_reconnectCts.Token);
                Logger.Log(LoggingLevel.Information, "重连成功");
                return;
            }
            catch
            {
                if (_options.MaxReconnectAttempts > 0 && attempts >= _options.MaxReconnectAttempts)
                {
                    Logger.Log(LoggingLevel.Warning, $"已达到最大重连次数 {_options.MaxReconnectAttempts}");
                    State = ModbusConnectionState.Disconnected;
                    return;
                }
                
                try
                {
                    await Task.Delay(_options.ReconnectInterval, _reconnectCts.Token);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }
        
        State = ModbusConnectionState.Disconnected;
    }
    
    private async Task<bool> EnsureConnectedAsync()
    {
        if (IsConnected && _master != null) return true;
        
        if (_options.AutoReconnect && State != ModbusConnectionState.Reconnecting)
        {
            Logger.Log(LoggingLevel.Warning, "连接已断开，尝试重连...");
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

    #region 读取操作
    
    /// <inheritdoc />
    public async Task<bool[]> ReadCoilsAsync(ushort startAddress, ushort count, CancellationToken cancellationToken = default)
    {
        if (!await EnsureConnectedAsync())
            throw new InvalidOperationException("未连接到从站");
        
        Logger.Log(LoggingLevel.Debug, $"读取线圈: 起始={startAddress}, 数量={count}");
        try
        {
            return await _master!.ReadCoilsAsync(SlaveId, startAddress, count);
        }
        catch (Exception ex)
        {
            await HandleCommunicationErrorAsync(ex);
            throw;
        }
    }
    
    /// <inheritdoc />
    public async Task<bool[]> ReadDiscreteInputsAsync(ushort startAddress, ushort count, CancellationToken cancellationToken = default)
    {
        if (!await EnsureConnectedAsync())
            throw new InvalidOperationException("未连接到从站");
        
        Logger.Log(LoggingLevel.Debug, $"读取离散输入: 起始={startAddress}, 数量={count}");
        try
        {
            return await _master!.ReadInputsAsync(SlaveId, startAddress, count);
        }
        catch (Exception ex)
        {
            await HandleCommunicationErrorAsync(ex);
            throw;
        }
    }
    
    /// <inheritdoc />
    public async Task<ushort[]> ReadHoldingRegistersAsync(ushort startAddress, ushort count, CancellationToken cancellationToken = default)
    {
        if (!await EnsureConnectedAsync())
            throw new InvalidOperationException("未连接到从站");
        
        Logger.Log(LoggingLevel.Debug, $"读取保持寄存器: 起始={startAddress}, 数量={count}");
        try
        {
            return await _master!.ReadHoldingRegistersAsync(SlaveId, startAddress, count);
        }
        catch (Exception ex)
        {
            await HandleCommunicationErrorAsync(ex);
            throw;
        }
    }
    
    /// <inheritdoc />
    public async Task<ushort[]> ReadInputRegistersAsync(ushort startAddress, ushort count, CancellationToken cancellationToken = default)
    {
        if (!await EnsureConnectedAsync())
            throw new InvalidOperationException("未连接到从站");
        
        Logger.Log(LoggingLevel.Debug, $"读取输入寄存器: 起始={startAddress}, 数量={count}");
        try
        {
            return await _master!.ReadInputRegistersAsync(SlaveId, startAddress, count);
        }
        catch (Exception ex)
        {
            await HandleCommunicationErrorAsync(ex);
            throw;
        }
    }
    
    #endregion
    
    #region 写入操作
    
    /// <inheritdoc />
    public async Task WriteSingleCoilAsync(ushort address, bool value, CancellationToken cancellationToken = default)
    {
        if (!await EnsureConnectedAsync())
            throw new InvalidOperationException("未连接到从站");
        
        Logger.Log(LoggingLevel.Debug, $"写入线圈: 地址={address}, 值={value}");
        try
        {
            await _master!.WriteSingleCoilAsync(SlaveId, address, value);
        }
        catch (Exception ex)
        {
            await HandleCommunicationErrorAsync(ex);
            throw;
        }
    }
    
    /// <inheritdoc />
    public async Task WriteSingleRegisterAsync(ushort address, ushort value, CancellationToken cancellationToken = default)
    {
        if (!await EnsureConnectedAsync())
            throw new InvalidOperationException("未连接到从站");
        
        Logger.Log(LoggingLevel.Debug, $"写入寄存器: 地址={address}, 值={value}");
        try
        {
            await _master!.WriteSingleRegisterAsync(SlaveId, address, value);
        }
        catch (Exception ex)
        {
            await HandleCommunicationErrorAsync(ex);
            throw;
        }
    }
    
    /// <inheritdoc />
    public async Task WriteMultipleCoilsAsync(ushort startAddress, bool[] values, CancellationToken cancellationToken = default)
    {
        if (!await EnsureConnectedAsync())
            throw new InvalidOperationException("未连接到从站");
        
        Logger.Log(LoggingLevel.Debug, $"写入多个线圈: 起始={startAddress}, 数量={values.Length}");
        try
        {
            await _master!.WriteMultipleCoilsAsync(SlaveId, startAddress, values);
        }
        catch (Exception ex)
        {
            await HandleCommunicationErrorAsync(ex);
            throw;
        }
    }
    
    /// <inheritdoc />
    public async Task WriteMultipleRegistersAsync(ushort startAddress, ushort[] values, CancellationToken cancellationToken = default)
    {
        if (!await EnsureConnectedAsync())
            throw new InvalidOperationException("未连接到从站");
        
        Logger.Log(LoggingLevel.Debug, $"写入多个寄存器: 起始={startAddress}, 数量={values.Length}");
        try
        {
            await _master!.WriteMultipleRegistersAsync(SlaveId, startAddress, values);
        }
        catch (Exception ex)
        {
            await HandleCommunicationErrorAsync(ex);
            throw;
        }
    }
    
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
            ReconnectingAsync = null;
            Heartbeat = null;
            HeartbeatAsync = null;
        }
        
        base.Dispose(disposing);
    }
}
