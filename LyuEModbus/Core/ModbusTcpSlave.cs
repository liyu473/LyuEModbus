using System.Net.Sockets;
using LyuEModbus.Abstractions;
using NModbus;

namespace LyuEModbus.Core;

/// <summary>
/// Modbus TCP 从站实现
/// </summary>
internal class ModbusTcpSlave : ModbusClientBase, Abstractions.IModbusSlave
{
    private TcpListener? _listener;
    private IModbusSlaveNetwork? _slaveNetwork;
    private NModbus.IModbusSlave? _slave;
    private CancellationTokenSource? _cts;
    private bool _disposed;
    private ushort[]? _lastHoldingValues;
    private bool[]? _lastCoilValues;
    private readonly List<TcpClient> _connectedClients = new();
    
    private readonly ModbusSlaveOptions _options;
    
    public override string Address => $"{_options.IpAddress}:{_options.Port}";
    
    /// <inheritdoc />
    public bool IsRunning { get; private set; }
    
    /// <inheritdoc />
    public ISlaveDataStore? DataStore => _slave?.DataStore;
    
    /// <inheritdoc />
    public event Action<ushort, ushort, ushort>? HoldingRegisterWritten;
    
    /// <inheritdoc />
    public event Func<ushort, ushort, ushort, Task>? HoldingRegisterWrittenAsync;
    
    /// <inheritdoc />
    public event Action<ushort, bool>? CoilWritten;
    
    /// <inheritdoc />
    public event Func<ushort, bool, Task>? CoilWrittenAsync;
    
    /// <inheritdoc />
    public event Action<string>? ClientConnected;
    
    /// <inheritdoc />
    public event Func<string, Task>? ClientConnectedAsync;
    
    /// <inheritdoc />
    public event Action<string>? ClientDisconnected;
    
    /// <inheritdoc />
    public event Func<string, Task>? ClientDisconnectedAsync;
    
    internal ModbusTcpSlave(string name, ModbusSlaveOptions options, IModbusLogger logger)
        : base(name, logger)
    {
        _options = options;
        SlaveId = options.SlaveId;
    }
    
    /// <inheritdoc />
    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (IsRunning)
        {
            Logger.Log(LoggingLevel.Debug, "从站已在运行中");
            return;
        }
        
        try
        {
            var ip = System.Net.IPAddress.Parse(_options.IpAddress);
            _listener = new TcpListener(ip, _options.Port);
            _listener.Start();
            
            var factory = new ModbusFactory();
            _slaveNetwork = factory.CreateSlaveNetwork(_listener);
            
            _slave = factory.CreateSlave(_options.SlaveId);
            _slaveNetwork.AddSlave(_slave);
            
            InitializeSimulationData();
            
            _cts = new CancellationTokenSource();
            
            _ = Task.Run(async () =>
            {
                try
                {
                    await _slaveNetwork.ListenAsync(_cts.Token);
                }
                catch (OperationCanceledException) { }
                catch (Exception ex)
                {
                    Logger.Log(LoggingLevel.Error, $"监听异常: {ex.Message}");
                }
            });
            
            _ = Task.Run(MonitorClientConnectionsAsync);
            _ = Task.Run(MonitorDataChangesAsync);
            
            IsRunning = true;
            State = ModbusConnectionState.Connected;
            Logger.Log(LoggingLevel.Information, $"从站已启动 - {Address}, SlaveId: {SlaveId}");
        }
        catch (Exception ex)
        {
            Logger.Log(LoggingLevel.Error, $"启动失败: {ex.Message}");
            throw;
        }
    }
    
    private void InitializeSimulationData()
    {
        if (_slave?.DataStore == null) return;
        
        _lastHoldingValues = new ushort[_options.InitHoldingRegisterCount];
        for (int i = 0; i < _lastHoldingValues.Length; i++)
            _lastHoldingValues[i] = (ushort)(i * 10);
        _slave.DataStore.HoldingRegisters.WritePoints(0, _lastHoldingValues);
        
        _lastCoilValues = new bool[_options.InitCoilCount];
        for (int i = 0; i < _lastCoilValues.Length; i++)
            _lastCoilValues[i] = i % 2 == 0;
        _slave.DataStore.CoilDiscretes.WritePoints(0, _lastCoilValues);
        
        Logger.Log(LoggingLevel.Debug, $"已初始化: {_options.InitHoldingRegisterCount}个寄存器, {_options.InitCoilCount}个线圈");
    }
    
    private async Task MonitorClientConnectionsAsync()
    {
        while (_cts != null && !_cts.Token.IsCancellationRequested && _listener != null)
        {
            try
            {
                if (_listener.Pending())
                {
                    var client = await _listener.AcceptTcpClientAsync(_cts.Token);
                    var endpoint = client.Client.RemoteEndPoint?.ToString() ?? "未知";
                    
                    lock (_connectedClients)
                        _connectedClients.Add(client);
                    
                    Logger.Log(LoggingLevel.Information, $"客户端已连接: {endpoint}");
                    ClientConnected?.Invoke(endpoint);
                    if (ClientConnectedAsync != null)
                        await ClientConnectedAsync.Invoke(endpoint);
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
                            
                            Logger.Log(LoggingLevel.Information, $"客户端已断开: {endpoint}");
                            ClientDisconnected?.Invoke(endpoint);
                            ClientDisconnectedAsync?.Invoke(endpoint);
                            
                            client.Close();
                        }
                    }
                }
                
                await Task.Delay(500, _cts.Token);
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex)
            {
                Logger.Log(LoggingLevel.Error, $"客户端监控异常: {ex.Message}");
            }
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
    
    private async Task MonitorDataChangesAsync()
    {
        while (_cts != null && !_cts.Token.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(_options.ChangeDetectionInterval, _cts.Token);
                await DetectChangesAsync();
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex)
            {
                Logger.Log(LoggingLevel.Error, $"监控异常: {ex.Message}");
            }
        }
    }
    
    private async Task DetectChangesAsync()
    {
        if (_slave?.DataStore == null || _lastHoldingValues == null || _lastCoilValues == null) return;
        
        var currentHolding = _slave.DataStore.HoldingRegisters.ReadPoints(0, (ushort)_lastHoldingValues.Length);
        for (int i = 0; i < currentHolding.Length; i++)
        {
            if (currentHolding[i] != _lastHoldingValues[i])
            {
                var address = (ushort)i;
                var oldValue = _lastHoldingValues[i];
                var newValue = currentHolding[i];
                
                Logger.Log(LoggingLevel.Debug, $"寄存器被修改: 地址={address}, {oldValue} -> {newValue}");
                HoldingRegisterWritten?.Invoke(address, oldValue, newValue);
                if (HoldingRegisterWrittenAsync != null)
                    await HoldingRegisterWrittenAsync.Invoke(address, oldValue, newValue);
                
                _lastHoldingValues[i] = newValue;
            }
        }
        
        var currentCoils = _slave.DataStore.CoilDiscretes.ReadPoints(0, (ushort)_lastCoilValues.Length);
        for (int i = 0; i < currentCoils.Length; i++)
        {
            if (currentCoils[i] != _lastCoilValues[i])
            {
                var address = (ushort)i;
                var value = currentCoils[i];
                
                Logger.Log(LoggingLevel.Debug, $"线圈被修改: 地址={address}, 值={value}");
                CoilWritten?.Invoke(address, value);
                if (CoilWrittenAsync != null)
                    await CoilWrittenAsync.Invoke(address, value);
                
                _lastCoilValues[i] = value;
            }
        }
    }
    
    /// <inheritdoc />
    public void Stop()
    {
        if (!IsRunning)
        {
            Logger.Log(LoggingLevel.Debug, "从站未运行");
            return;
        }
        
        try
        {
            _cts?.Cancel();
            _slaveNetwork?.Dispose();
            _listener?.Stop();
            
            _cts = null;
            _slaveNetwork = null;
            _slave = null;
            _listener = null;
            _lastHoldingValues = null;
            _lastCoilValues = null;
            
            IsRunning = false;
            State = ModbusConnectionState.Disconnected;
            Logger.Log(LoggingLevel.Information, "从站已停止");
        }
        catch (Exception ex)
        {
            Logger.Log(LoggingLevel.Error, $"停止失败: {ex.Message}");
            throw;
        }
    }
    
    /// <inheritdoc />
    public void SetCoil(ushort address, bool value)
    {
        _slave?.DataStore?.CoilDiscretes.WritePoints(address, new[] { value });
        if (_lastCoilValues != null && address < _lastCoilValues.Length)
            _lastCoilValues[address] = value;
    }
    
    /// <inheritdoc />
    public void SetHoldingRegister(ushort address, ushort value)
    {
        _slave?.DataStore?.HoldingRegisters.WritePoints(address, new[] { value });
        if (_lastHoldingValues != null && address < _lastHoldingValues.Length)
            _lastHoldingValues[address] = value;
    }
    
    /// <inheritdoc />
    public void SetHoldingRegisters(ushort startAddress, ushort[] values)
    {
        _slave?.DataStore?.HoldingRegisters.WritePoints(startAddress, values);
        if (_lastHoldingValues != null)
        {
            for (int i = 0; i < values.Length && startAddress + i < _lastHoldingValues.Length; i++)
                _lastHoldingValues[startAddress + i] = values[i];
        }
    }
    
    /// <inheritdoc />
    public ushort[]? ReadHoldingRegisters(ushort startAddress, ushort count)
    {
        return _slave?.DataStore?.HoldingRegisters.ReadPoints(startAddress, count);
    }
    
    protected override void Dispose(bool disposing)
    {
        if (_disposed) return;
        _disposed = true;
        
        if (disposing)
        {
            Stop();
            
            HoldingRegisterWritten = null;
            HoldingRegisterWrittenAsync = null;
            CoilWritten = null;
            CoilWrittenAsync = null;
            ClientConnected = null;
            ClientConnectedAsync = null;
            ClientDisconnected = null;
            ClientDisconnectedAsync = null;
        }
        
        base.Dispose(disposing);
    }
}
