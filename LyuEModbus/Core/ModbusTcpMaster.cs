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
        SlaveId = options.SlaveId;
    }

    public override async Task ConnectAsync(CancellationToken cancellationToken = default)
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
            InternalMaster = factory.CreateMaster(_client);
            InternalMaster.Transport.ReadTimeout = _options.ReadTimeout;
            InternalMaster.Transport.WriteTimeout = _options.WriteTimeout;

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
                    OnHeartbeat();

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

        State = ModbusConnectionState.Reconnecting;
        _reconnectCts = new CancellationTokenSource();
        var attempts = 0;

        while (!_reconnectCts.Token.IsCancellationRequested)
        {
            attempts++;
            OnReconnecting(attempts);
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
