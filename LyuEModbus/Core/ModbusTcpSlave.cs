using LyuEModbus.Models;
using NModbus;
using System.Net.Sockets;

namespace LyuEModbus.Core;

/// <summary>
/// Modbus TCP 从站实现
/// </summary>
internal class ModbusTcpSlave : ModbusSlaveBase
{
    private TcpListener? _listener;
    private IModbusSlaveNetwork? _slaveNetwork;
    private CancellationTokenSource? _cts;
    private bool _disposed;
    private ushort[]? _lastHoldingValues;
    private bool[]? _lastCoilValues;
    private readonly List<TcpClient> _connectedClients = [];

    private readonly ModbusSlaveOptions _options;

    public override string Address => $"{_options.IpAddress}:{_options.Port}";

    internal ModbusTcpSlave(string name, ModbusSlaveOptions options, IModbusLogger logger)
        : base(name, options.SlaveId ?? 1, logger)
    {
        _options = options;
    }

    private void ValidateOptions()
    {
        if (string.IsNullOrWhiteSpace(_options.IpAddress))
            throw new InvalidOperationException("未配置监听 IP 地址，请先调用 WithEndpoint()");
        if (!_options.Port.HasValue)
            throw new InvalidOperationException("未配置监听端口，请先调用 WithEndpoint()");
        if (!_options.SlaveId.HasValue)
            throw new InvalidOperationException("未配置从站 ID，请先调用 WithSlaveId()");
        if (!_options.InitHoldingRegisterCount.HasValue)
            throw new InvalidOperationException("未配置初始保持寄存器数量，请先调用 WithDataStore()");
        if (!_options.InitCoilCount.HasValue)
            throw new InvalidOperationException("未配置初始线圈数量，请先调用 WithDataStore()");
    }

    internal void ConfigureEndpoint(string ipAddress, int port)
    {
        _options.IpAddress = ipAddress;
        _options.Port = port;
    }

    internal void ConfigureSlaveId(byte slaveId)
    {
        _options.SlaveId = slaveId;
    }

    internal void ConfigureDataStore(ushort holdingRegisterCount, ushort coilCount)
    {
        _options.InitHoldingRegisterCount = holdingRegisterCount;
        _options.InitCoilCount = coilCount;
    }

    public override async Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (IsRunning) return;
        ValidateOptions();

        try
        {
            var ip = System.Net.IPAddress.Parse(_options.IpAddress!);
            _listener = new TcpListener(ip, _options.Port!.Value);
            _listener.Start();

            var factory = new ModbusFactory();
            _slaveNetwork = factory.CreateSlaveNetwork(_listener);
            InternalSlave = factory.CreateSlave(_options.SlaveId!.Value);
            _slaveNetwork.AddSlave(InternalSlave);

            InitializeData();
            _cts = new CancellationTokenSource();

            _ = Task.Run(async () =>
            {
                try { await _slaveNetwork.ListenAsync(_cts.Token); }
                catch (OperationCanceledException) { }
                catch (Exception ex) { Logger.Log(LoggingLevel.Error, $"监听异常: {ex.Message}"); }
            }, _cts.Token);

            _ = Task.Run(MonitorClientsAsync, _cts.Token);
            _ = Task.Run(MonitorChangesAsync, _cts.Token);

            IsRunning = true;
            Logger.Log(LoggingLevel.Information, $"从站已启动 - {Address}");
        }
        catch (Exception ex)
        {
            Logger.Log(LoggingLevel.Error, $"启动失败: {ex.Message}");
            throw;
        }
    }

    private void InitializeData()
    {
        if (InternalSlave?.DataStore == null) return;

        _lastHoldingValues = new ushort[_options.InitHoldingRegisterCount!.Value];
        for (int i = 0; i < _lastHoldingValues.Length; i++)
            _lastHoldingValues[i] = (ushort)(i * 10);
        InternalSlave.DataStore.HoldingRegisters.WritePoints(0, _lastHoldingValues);

        _lastCoilValues = new bool[_options.InitCoilCount!.Value];
        for (int i = 0; i < _lastCoilValues.Length; i++)
            _lastCoilValues[i] = i % 2 == 0;
        InternalSlave.DataStore.CoilDiscretes.WritePoints(0, _lastCoilValues);
    }

    private async Task MonitorClientsAsync()
    {
        while (_cts != null && !_cts.Token.IsCancellationRequested && _listener != null)
        {
            try
            {
                if (_listener.Pending())
                {
                    var client = await _listener.AcceptTcpClientAsync(_cts.Token);
                    var endpoint = client.Client.RemoteEndPoint?.ToString() ?? "未知";
                    lock (_connectedClients) _connectedClients.Add(client);
                    Logger.Log(LoggingLevel.Information, $"客户端连接: {endpoint}");
                    OnClientConnected(endpoint);
                }

                lock (_connectedClients)
                {
                    for (int i = _connectedClients.Count - 1; i >= 0; i--)
                    {
                        var client = _connectedClients[i];
                        if (!IsClientConnected(client))
                        {
                            var endpoint = client.Client.RemoteEndPoint?.ToString() ?? "未知";
                            _connectedClients.RemoveAt(i);
                            Logger.Log(LoggingLevel.Information, $"客户端断开: {endpoint}");
                            OnClientDisconnected(endpoint);
                            client.Close();
                        }
                    }
                }
                await Task.Delay(500, _cts.Token);
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex) { Logger.Log(LoggingLevel.Error, $"监控异常: {ex.Message}"); }
        }
    }

    private static bool IsClientConnected(TcpClient client)
    {
        try
        {
            if (client.Client == null || !client.Connected) return false;
            if (client.Client.Poll(0, SelectMode.SelectRead))
            {
                byte[] buff = new byte[1];
                return client.Client.Receive(buff, SocketFlags.Peek) != 0;
            }
            return true;
        }
        catch { return false; }
    }

    private async Task MonitorChangesAsync()
    {
        while (_cts != null && !_cts.Token.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(_options.ChangeDetectionInterval, _cts.Token);
                DetectChanges();
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex) { Logger.Log(LoggingLevel.Error, $"检测异常: {ex.Message}"); }
        }
    }

    private void DetectChanges()
    {
        if (InternalSlave?.DataStore == null || _lastHoldingValues == null || _lastCoilValues == null) return;

        var currentHolding = InternalSlave.DataStore.HoldingRegisters.ReadPoints(0, (ushort)_lastHoldingValues.Length);
        for (int i = 0; i < currentHolding.Length; i++)
        {
            if (currentHolding[i] != _lastHoldingValues[i])
            {
                OnHoldingRegisterWritten((ushort)i, _lastHoldingValues[i], currentHolding[i]);
                _lastHoldingValues[i] = currentHolding[i];
            }
        }

        var currentCoils = InternalSlave.DataStore.CoilDiscretes.ReadPoints(0, (ushort)_lastCoilValues.Length);
        for (int i = 0; i < currentCoils.Length; i++)
        {
            if (currentCoils[i] != _lastCoilValues[i])
            {
                OnCoilWritten((ushort)i, currentCoils[i]);
                _lastCoilValues[i] = currentCoils[i];
            }
        }
    }

    public override void Stop()
    {
        if (!IsRunning) return;

        _cts?.Cancel();
        _slaveNetwork?.Dispose();
        _listener?.Stop();

        _cts = null;
        _slaveNetwork = null;
        InternalSlave = null;
        _listener = null;
        _lastHoldingValues = null;
        _lastCoilValues = null;

        IsRunning = false;
        Logger.Log(LoggingLevel.Information, "从站已停止");
    }

    public override void SetCoil(ushort address, bool value)
    {
        InternalSlave?.DataStore?.CoilDiscretes.WritePoints(address, [value]);
        if (_lastCoilValues != null && address < _lastCoilValues.Length)
            _lastCoilValues[address] = value;
    }

    public override void SetHoldingRegister(ushort address, ushort value)
    {
        InternalSlave?.DataStore?.HoldingRegisters.WritePoints(address, [value]);
        if (_lastHoldingValues != null && address < _lastHoldingValues.Length)
            _lastHoldingValues[address] = value;
    }

    public override void SetHoldingRegisters(ushort startAddress, ushort[] values)
    {
        InternalSlave?.DataStore?.HoldingRegisters.WritePoints(startAddress, values);
        if (_lastHoldingValues != null)
            for (int i = 0; i < values.Length && startAddress + i < _lastHoldingValues.Length; i++)
                _lastHoldingValues[startAddress + i] = values[i];
    }

    public override ushort[]? ReadHoldingRegisters(ushort startAddress, ushort count)
        => InternalSlave?.DataStore?.HoldingRegisters.ReadPoints(startAddress, count);

    protected override void Dispose(bool disposing)
    {
        if (_disposed) return;
        _disposed = true;

        if (disposing)
            Stop();

        base.Dispose(disposing);
    }
}
