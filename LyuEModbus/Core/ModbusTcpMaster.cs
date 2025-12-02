using LyuEModbus.Models;
using NModbus;
using System.Net.Sockets;

namespace LyuEModbus.Core;

/// <summary>
/// Modbus TCP 主站实现
/// </summary>
internal class ModbusTcpMaster : ModbusMasterBase
{
    private TcpClient? _client;
    private CancellationTokenSource? _reconnectCts;
    private CancellationTokenSource? _heartbeatCts;
    private bool _disposed;

    private readonly ModbusMasterOptions _options;

    public override string Address => $"{_options.IpAddress}:{_options.Port}";

    internal ModbusTcpMaster(string name, ModbusMasterOptions options, IModbusLogger logger)
        : base(name, logger)
    {
        _options = options;
        if (options.SlaveId.HasValue)
            SlaveId = options.SlaveId.Value;
    }

    public override async Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        if (IsConnected)
        {
            Logger.Log(LoggingLevel.Debug, "已连接，跳过");
            return;
        }
        ValidateOptions();
        await ConnectInternalAsync(isReconnecting: false, cancellationToken);
    }

    private void ValidateOptions()
    {
        if (string.IsNullOrWhiteSpace(_options.IpAddress))
            throw new InvalidOperationException("未配置 IP 地址，请先调用 WithEndpoint()");
        if (!_options.Port.HasValue)
            throw new InvalidOperationException("未配置端口，请先调用 WithEndpoint()");
        if (!_options.SlaveId.HasValue)
            throw new InvalidOperationException("未配置从站 ID，请先调用 WithSlaveId()");
        if (!_options.ReadTimeout.HasValue || !_options.WriteTimeout.HasValue)
            throw new InvalidOperationException("未配置超时时间，请先调用 WithTimeout()");
    }

    private async Task ConnectInternalAsync(bool isReconnecting = false, CancellationToken cancellationToken = default)
    {
        // 重连期间保持 Reconnecting 状态，不切换到 Connecting
        if (!isReconnecting)
            State = ModbusConnectionState.Connecting;

        try
        {
            _client = new TcpClient();
            await _client.ConnectAsync(_options.IpAddress!, _options.Port!.Value, cancellationToken);

            _client.ReceiveTimeout = _options.ReadTimeout!.Value;
            _client.SendTimeout = _options.WriteTimeout!.Value;

            var factory = new ModbusFactory();
            InternalMaster = factory.CreateMaster(_client);
            InternalMaster.Transport.ReadTimeout = _options.ReadTimeout!.Value;
            InternalMaster.Transport.WriteTimeout = _options.WriteTimeout!.Value;

            _reconnectCts = null; // 连接成功，清理重连令牌
            State = ModbusConnectionState.Connected;
            Logger.Log(LoggingLevel.Information, $"已连接到 {Address}");

            if (_options.EnableHeartbeat)
                StartHeartbeat();
        }
        catch (Exception ex)
        {
            // 重连期间失败不改变状态，由 StartReconnectAsync 控制
            if (!isReconnecting)
                State = ModbusConnectionState.Disconnected;
            Logger.Log(LoggingLevel.Error, $"连接失败: {ex.Message}");
            throw;
        }
    }

    public override void Disconnect()
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
            InternalMaster?.Dispose();
            _client?.Close();
            InternalMaster = null;
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

    public override void StopReconnect()
    {
        _reconnectCts?.Cancel();
        _reconnectCts = null;
    }

    internal void ConfigureEndpoint(string ipAddress, int port)
    {
        _options.IpAddress = ipAddress;
        _options.Port = port;
    }

    internal void ConfigureSlaveId(byte slaveId)
    {
        _options.SlaveId = slaveId;
        base.SlaveId = slaveId;
    }

    internal void ConfigureTimeout(int readTimeoutMs, int writeTimeoutMs)
    {
        _options.ReadTimeout = readTimeoutMs;
        _options.WriteTimeout = writeTimeoutMs;
    }

    internal void ConfigureReconnect(bool enabled, int intervalMs = 5000, int maxAttempts = 0)
    {
        _options.AutoReconnect = enabled;
        _options.ReconnectInterval = intervalMs;
        _options.MaxReconnectAttempts = maxAttempts;
    }

    internal void ConfigureHeartbeat(bool enabled, int intervalMs = 5000)
    {
        _options.EnableHeartbeat = enabled;
        _options.HeartbeatInterval = intervalMs;
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
                    await OnHeartbeatAsync();

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
        try { InternalMaster?.Dispose(); _client?.Close(); } catch { }
        InternalMaster = null;
        _client = null;
    }

    private async Task StartReconnectAsync()
    {
        if (!_options.AutoReconnect) return;
        if (_reconnectCts != null && !_reconnectCts.IsCancellationRequested) return;

        _reconnectCts = new CancellationTokenSource();
        State = ModbusConnectionState.Reconnecting;
        var attempts = 0;
        var maxAttempts = _options.MaxReconnectAttempts;
        var maxDisplay = maxAttempts == 0 ? "∞" : maxAttempts.ToString();

        while (!_reconnectCts.Token.IsCancellationRequested)
        {
            attempts++;
            await OnReconnectingAsync(attempts, maxAttempts); // 传递当前次数和最大次数
            Logger.Log(LoggingLevel.Information, $"重连 {attempts}/{maxDisplay}");

            try
            {
                await ConnectInternalAsync(isReconnecting: true, _reconnectCts.Token);
                Logger.Log(LoggingLevel.Information, "重连成功");
                return;
            }
            catch
            {
                CleanupConnection(); // 确保清理失败的连接
                
                if (maxAttempts > 0 && attempts >= maxAttempts)
                {
                    Logger.Log(LoggingLevel.Warning, $"重连失败，已达到最大重连次数 {maxAttempts}");
                    await OnReconnectFailedAsync(); // 通知重连失败
                    State = ModbusConnectionState.Disconnected;
                    _reconnectCts = null;
                    return;
                }
                
                try 
                { 
                    await Task.Delay(_options.ReconnectInterval, _reconnectCts.Token); 
                }
                catch (OperationCanceledException) 
                { 
                    Logger.Log(LoggingLevel.Information, "重连已取消");
                    break; 
                }
            }
        }
        
        State = ModbusConnectionState.Disconnected;
        _reconnectCts = null;
    }

    protected override void Dispose(bool disposing)
    {
        if (_disposed) return;
        _disposed = true;

        if (disposing)
        {
            StopReconnect();
            StopHeartbeat();
            CleanupConnection();
        }
        base.Dispose(disposing);
    }
}
