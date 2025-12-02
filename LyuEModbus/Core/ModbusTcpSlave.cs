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
            InternalSlave = factory.CreateSlave(_options.SlaveId!.Value);

            InitializeData();
            _cts = new CancellationTokenSource();

            // 自己管理 TCP 连接，然后传递给 NModbus 处理
            _ = Task.Run(AcceptClientsAsync, _cts.Token);
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

    /// <summary>
    /// 接受客户端连接并传递给 NModbus 处理
    /// </summary>
    private async Task AcceptClientsAsync()
    {
        Logger.Log(LoggingLevel.Debug, "开始监听客户端连接...");
        
        while (_cts != null && !_cts.Token.IsCancellationRequested && _listener != null)
        {
            try
            {
                // 接受新连接
                Logger.Log(LoggingLevel.Debug, "等待客户端连接...");
                var client = await _listener.AcceptTcpClientAsync(_cts.Token);
                var endpoint = client.Client.RemoteEndPoint?.ToString() ?? "未知";
                
                lock (_connectedClients) 
                    _connectedClients.Add(client);
                
                Logger.Log(LoggingLevel.Information, $"客户端连接: {endpoint}");
                
                // 触发连接事件
                try
                {
                    OnClientConnected(endpoint);
                    Logger.Log(LoggingLevel.Debug, $"已触发 ClientConnected 事件: {endpoint}");
                }
                catch (Exception eventEx)
                {
                    Logger.Log(LoggingLevel.Error, $"ClientConnected 事件处理异常: {eventEx.Message}");
                }
                
                // 启动单独的任务处理此客户端的 Modbus 请求
                _ = HandleClientAsync(client, endpoint);
            }
            catch (OperationCanceledException) { break; }
            catch (ObjectDisposedException) { break; }
            catch (Exception ex) 
            { 
                Logger.Log(LoggingLevel.Error, $"接受连接异常: {ex.Message}"); 
            }
        }
        
        Logger.Log(LoggingLevel.Debug, "停止监听客户端连接");
    }
    
    /// <summary>
    /// 处理单个客户端的 Modbus 请求
    /// </summary>
    private async Task HandleClientAsync(TcpClient client, string endpoint)
    {
        try
        {
            var stream = client.GetStream();
            var buffer = new byte[256];
            
            while (_cts != null && !_cts.Token.IsCancellationRequested && IsClientConnected(client))
            {
                try
                {
                    // 读取 MBAP 头 (7 bytes) + PDU
                    var bytesRead = await stream.ReadAsync(buffer.AsMemory(), _cts.Token);
                    if (bytesRead == 0) break; // 客户端断开
                    
                    if (bytesRead >= 8 && InternalSlave != null)
                    {
                        // 解析 Modbus TCP 请求 (MBAP Header + PDU)
                        var transactionId = (ushort)((buffer[0] << 8) | buffer[1]);
                        // buffer[2..3] = Protocol ID, buffer[4..5] = Length
                        var unitId = buffer[6]; // 用于响应
                        var functionCode = buffer[7];
                        
                        // 构建响应
                        var responseData = ProcessModbusRequest(functionCode, buffer, 8);
                        if (responseData != null)
                        {
                            // 构建 MBAP 头 + 响应
                            var response = new byte[7 + responseData.Length];
                            response[0] = (byte)(transactionId >> 8);
                            response[1] = (byte)(transactionId & 0xFF);
                            response[2] = 0; // Protocol ID
                            response[3] = 0;
                            response[4] = (byte)((responseData.Length + 1) >> 8);
                            response[5] = (byte)((responseData.Length + 1) & 0xFF);
                            response[6] = unitId;
                            Array.Copy(responseData, 0, response, 7, responseData.Length);
                            
                            await stream.WriteAsync(response.AsMemory(), _cts.Token);
                        }
                    }
                }
                catch (IOException) { break; }
                catch (OperationCanceledException) { break; }
            }
        }
        catch (OperationCanceledException) { }
        catch (IOException) { }
        catch (Exception ex)
        {
            Logger.Log(LoggingLevel.Error, $"处理客户端异常: {ex.Message}");
        }
        finally
        {
            lock (_connectedClients)
            {
                if (_connectedClients.Remove(client))
                {
                    Logger.Log(LoggingLevel.Information, $"客户端断开: {endpoint}");
                    OnClientDisconnected(endpoint);
                }
            }
            try { client.Close(); } catch { }
        }
    }
    
    /// <summary>
    /// 处理 Modbus 请求并返回响应数据（不含 Unit ID）
    /// </summary>
    private byte[]? ProcessModbusRequest(byte functionCode, byte[] data, int offset)
    {
        if (InternalSlave?.DataStore == null) return null;
        
        try
        {
            return functionCode switch
            {
                0x03 => ProcessReadHoldingRegisters(data, offset),          // Read Holding Registers
                0x06 => ProcessWriteSingleRegister(data, offset),           // Write Single Register
                0x10 => ProcessWriteMultipleRegisters(data, offset),        // Write Multiple Registers
                0x01 => ProcessReadCoils(data, offset),                     // Read Coils
                0x05 => ProcessWriteSingleCoil(data, offset),               // Write Single Coil
                0x0F => ProcessWriteMultipleCoils(data, offset),            // Write Multiple Coils
                _ => [(byte)(functionCode | 0x80), 0x01]                    // 不支持的功能码
            };
        }
        catch
        {
            return [(byte)(functionCode | 0x80), 0x04]; // Slave Device Failure
        }
    }
    
    private byte[] ProcessReadHoldingRegisters(byte[] data, int offset)
    {
        var startAddress = (ushort)((data[offset] << 8) | data[offset + 1]);
        var quantity = (ushort)((data[offset + 2] << 8) | data[offset + 3]);
        var values = InternalSlave!.DataStore!.HoldingRegisters.ReadPoints(startAddress, quantity);
        
        var response = new byte[2 + quantity * 2];
        response[0] = 0x03;
        response[1] = (byte)(quantity * 2);
        for (int i = 0; i < values.Length; i++)
        {
            response[2 + i * 2] = (byte)(values[i] >> 8);
            response[3 + i * 2] = (byte)(values[i] & 0xFF);
        }
        return response;
    }
    
    private byte[] ProcessWriteSingleRegister(byte[] data, int offset)
    {
        var address = (ushort)((data[offset] << 8) | data[offset + 1]);
        var value = (ushort)((data[offset + 2] << 8) | data[offset + 3]);
        InternalSlave!.DataStore!.HoldingRegisters.WritePoints(address, [value]);
        
        return [0x06, data[offset], data[offset + 1], data[offset + 2], data[offset + 3]];
    }
    
    private byte[] ProcessWriteMultipleRegisters(byte[] data, int offset)
    {
        var startAddress = (ushort)((data[offset] << 8) | data[offset + 1]);
        var quantity = (ushort)((data[offset + 2] << 8) | data[offset + 3]);
        _ = data[offset + 4]; // byteCount (用于验证但此处跳过)
        
        var values = new ushort[quantity];
        for (int i = 0; i < quantity; i++)
        {
            values[i] = (ushort)((data[offset + 5 + i * 2] << 8) | data[offset + 6 + i * 2]);
        }
        InternalSlave!.DataStore!.HoldingRegisters.WritePoints(startAddress, values);
        
        return [0x10, data[offset], data[offset + 1], data[offset + 2], data[offset + 3]];
    }
    
    private byte[] ProcessReadCoils(byte[] data, int offset)
    {
        var startAddress = (ushort)((data[offset] << 8) | data[offset + 1]);
        var quantity = (ushort)((data[offset + 2] << 8) | data[offset + 3]);
        var values = InternalSlave!.DataStore!.CoilDiscretes.ReadPoints(startAddress, quantity);
        
        var byteCount = (quantity + 7) / 8;
        var response = new byte[2 + byteCount];
        response[0] = 0x01;
        response[1] = (byte)byteCount;
        
        for (int i = 0; i < quantity; i++)
        {
            if (values[i])
                response[2 + i / 8] |= (byte)(1 << (i % 8));
        }
        return response;
    }
    
    private byte[] ProcessWriteSingleCoil(byte[] data, int offset)
    {
        var address = (ushort)((data[offset] << 8) | data[offset + 1]);
        var value = data[offset + 2] == 0xFF;
        InternalSlave!.DataStore!.CoilDiscretes.WritePoints(address, [value]);
        
        return [0x05, data[offset], data[offset + 1], data[offset + 2], data[offset + 3]];
    }
    
    private byte[] ProcessWriteMultipleCoils(byte[] data, int offset)
    {
        var startAddress = (ushort)((data[offset] << 8) | data[offset + 1]);
        var quantity = (ushort)((data[offset + 2] << 8) | data[offset + 3]);
        _ = data[offset + 4]; // byteCount (用于验证但此处跳过)
        
        var values = new bool[quantity];
        for (int i = 0; i < quantity; i++)
        {
            values[i] = (data[offset + 5 + i / 8] & (1 << (i % 8))) != 0;
        }
        InternalSlave!.DataStore!.CoilDiscretes.WritePoints(startAddress, values);
        
        return [0x0F, data[offset], data[offset + 1], data[offset + 2], data[offset + 3]];
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
        _listener?.Stop();
        
        // 关闭所有连接的客户端
        lock (_connectedClients)
        {
            foreach (var client in _connectedClients)
            {
                try { client.Close(); } catch { }
            }
            _connectedClients.Clear();
        }

        _cts = null;
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
