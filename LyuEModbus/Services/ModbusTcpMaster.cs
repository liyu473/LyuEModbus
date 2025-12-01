using System.Net.Sockets;
using NModbus;

namespace LyuEModbus.Services;

/// <summary>
/// Modbus TCP 主站封装
/// </summary>
public class ModbusTcpMaster : IDisposable
{
    private TcpClient? _client;
    private IModbusMaster? _master;
    private bool _disposed;
    private CancellationTokenSource? _reconnectCts;
    private bool _isReconnecting;

    /// <summary>
    /// 从站IP地址
    /// </summary>
    public string IpAddress { get; internal set; } = "127.0.0.1";

    /// <summary>
    /// 端口号
    /// </summary>
    public int Port { get; internal set; } = 502;

    /// <summary>
    /// 从站ID
    /// </summary>
    public byte SlaveId { get; internal set; } = 1;

    /// <summary>
    /// 读取超时（毫秒）
    /// </summary>
    public int ReadTimeout { get; internal set; } = 3000;

    /// <summary>
    /// 写入超时（毫秒）
    /// </summary>
    public int WriteTimeout { get; internal set; } = 3000;

    /// <summary>
    /// 是否启用自动重连
    /// </summary>
    public bool AutoReconnect { get; internal set; } = false;

    /// <summary>
    /// 重连间隔（毫秒）
    /// </summary>
    public int ReconnectInterval { get; internal set; } = 5000;

    /// <summary>
    /// 最大重连次数（0表示无限）
    /// </summary>
    public int MaxReconnectAttempts { get; internal set; } = 0;

    /// <summary>
    /// 是否已连接
    /// </summary>
    public bool IsConnected => _client?.Connected ?? false;

    /// <summary>
    /// 是否正在重连
    /// </summary>
    public bool IsReconnecting => _isReconnecting;

    /// <summary>
    /// 日志事件
    /// </summary>
    public event Action<string>? OnLog;

    /// <summary>
    /// 异步日志事件
    /// </summary>
    public event Func<string, Task>? OnLogAsync;

    /// <summary>
    /// 连接状态变化事件
    /// </summary>
    public event Action<bool>? OnConnectionChanged;

    /// <summary>
    /// 异步连接状态变化事件
    /// </summary>
    public event Func<bool, Task>? OnConnectionChangedAsync;

    /// <summary>
    /// 重连事件 (当前重连次数)
    /// </summary>
    public event Action<int>? OnReconnecting;

    /// <summary>
    /// 异步重连事件 (当前重连次数)
    /// </summary>
    public event Func<int, Task>? OnReconnectingAsync;

    /// <summary>
    /// 创建 Modbus TCP 主站
    /// </summary>
    public ModbusTcpMaster(string ipAddress = "127.0.0.1", int port = 502, byte slaveId = 1, int readTimeout = 3000, int writeTimeout = 3000)
    {
        IpAddress = ipAddress;
        Port = port;
        SlaveId = slaveId;
        ReadTimeout = readTimeout;
        WriteTimeout = writeTimeout;
    }

    /// <summary>
    /// 创建主站构建器
    /// </summary>
    public static ModbusTcpMaster Create() => new();

    /// <summary>
    /// 连接到从站
    /// </summary>
    public async Task<ModbusTcpMaster> ConnectAsync()
    {
        if (IsConnected)
        {
            Log("已连接");
            return this;
        }

        await ConnectInternalAsync();
        return this;
    }

    private async Task ConnectInternalAsync()
    {
        try
        {
            _client = new TcpClient();
            await _client.ConnectAsync(IpAddress, Port);

            _client.ReceiveTimeout = ReadTimeout;
            _client.SendTimeout = WriteTimeout;

            var factory = new ModbusFactory();
            _master = factory.CreateMaster(_client);
            _master.Transport.ReadTimeout = ReadTimeout;
            _master.Transport.WriteTimeout = WriteTimeout;

            _isReconnecting = false;
            OnConnectionChanged?.Invoke(true);
            OnConnectionChangedAsync?.Invoke(true);
            Log($"已连接到 {IpAddress}:{Port}");
        }
        catch (Exception ex)
        {
            Log($"连接失败: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// 断开连接
    /// </summary>
    public void Disconnect()
    {
        StopReconnect();

        if (!IsConnected && _client == null)
        {
            Log("未连接");
            return;
        }

        try
        {
            _master?.Dispose();
            _client?.Close();
            _master = null;
            _client = null;

            OnConnectionChanged?.Invoke(false);
            OnConnectionChangedAsync?.Invoke(false);
            Log("已断开连接");
        }
        catch (Exception ex)
        {
            Log($"断开失败: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// 停止重连
    /// </summary>
    public void StopReconnect()
    {
        _reconnectCts?.Cancel();
        _reconnectCts = null;
        _isReconnecting = false;
    }

    /// <summary>
    /// 启动自动重连
    /// </summary>
    private async Task StartReconnectAsync()
    {
        if (!AutoReconnect || _isReconnecting) return;

        _isReconnecting = true;
        _reconnectCts = new CancellationTokenSource();
        var attempts = 0;

        Log("开始自动重连...");

        while (!_reconnectCts.Token.IsCancellationRequested)
        {
            attempts++;
            OnReconnecting?.Invoke(attempts);
            if (OnReconnectingAsync != null)
            {
                await OnReconnectingAsync.Invoke(attempts);
            }
            Log($"重连尝试 {attempts}/{(MaxReconnectAttempts == 0 ? "∞" : MaxReconnectAttempts.ToString())}");

            try
            {
                await ConnectInternalAsync();
                Log("重连成功");
                return;
            }
            catch
            {
                if (MaxReconnectAttempts > 0 && attempts >= MaxReconnectAttempts)
                {
                    Log($"已达到最大重连次数 {MaxReconnectAttempts}，停止重连");
                    _isReconnecting = false;
                    return;
                }

                try
                {
                    await Task.Delay(ReconnectInterval, _reconnectCts.Token);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }

        _isReconnecting = false;
    }

    /// <summary>
    /// 检查连接并尝试重连
    /// </summary>
    private async Task<bool> EnsureConnectedAsync()
    {
        if (IsConnected && _master != null) return true;

        if (AutoReconnect && !_isReconnecting)
        {
            OnConnectionChanged?.Invoke(false);
            OnConnectionChangedAsync?.Invoke(false);
            Log("连接已断开，尝试重连...");
            
            // 清理旧连接
            _master?.Dispose();
            _client?.Close();
            _master = null;
            _client = null;

            await StartReconnectAsync();
            return IsConnected;
        }

        return false;
    }

    #region 读取操作

    /// <summary>
    /// 读取线圈 (功能码 01)
    /// </summary>
    public async Task<bool[]> ReadCoilsAsync(ushort startAddress, ushort numberOfPoints)
    {
        if (!await EnsureConnectedAsync())
            throw new InvalidOperationException("未连接到从站");

        Log($"读取线圈: 起始地址={startAddress}, 数量={numberOfPoints}");
        try
        {
            var result = await _master!.ReadCoilsAsync(SlaveId, startAddress, numberOfPoints);
            Log($"读取结果: [{string.Join(", ", result)}]");
            return result;
        }
        catch (Exception ex)
        {
            await HandleCommunicationErrorAsync(ex);
            throw;
        }
    }

    /// <summary>
    /// 读取离散输入 (功能码 02)
    /// </summary>
    public async Task<bool[]> ReadInputsAsync(ushort startAddress, ushort numberOfPoints)
    {
        if (!await EnsureConnectedAsync())
            throw new InvalidOperationException("未连接到从站");

        Log($"读取离散输入: 起始地址={startAddress}, 数量={numberOfPoints}");
        try
        {
            var result = await _master!.ReadInputsAsync(SlaveId, startAddress, numberOfPoints);
            Log($"读取结果: [{string.Join(", ", result)}]");
            return result;
        }
        catch (Exception ex)
        {
            await HandleCommunicationErrorAsync(ex);
            throw;
        }
    }

    /// <summary>
    /// 读取保持寄存器 (功能码 03)
    /// </summary>
    public async Task<ushort[]> ReadHoldingRegistersAsync(ushort startAddress, ushort numberOfPoints)
    {
        if (!await EnsureConnectedAsync())
            throw new InvalidOperationException("未连接到从站");

        Log($"读取保持寄存器: 起始地址={startAddress}, 数量={numberOfPoints}");
        try
        {
            var result = await _master!.ReadHoldingRegistersAsync(SlaveId, startAddress, numberOfPoints);
            Log($"读取结果: [{string.Join(", ", result)}]");
            return result;
        }
        catch (Exception ex)
        {
            await HandleCommunicationErrorAsync(ex);
            throw;
        }
    }

    /// <summary>
    /// 读取输入寄存器 (功能码 04)
    /// </summary>
    public async Task<ushort[]> ReadInputRegistersAsync(ushort startAddress, ushort numberOfPoints)
    {
        if (!await EnsureConnectedAsync())
            throw new InvalidOperationException("未连接到从站");

        Log($"读取输入寄存器: 起始地址={startAddress}, 数量={numberOfPoints}");
        try
        {
            var result = await _master!.ReadInputRegistersAsync(SlaveId, startAddress, numberOfPoints);
            Log($"读取结果: [{string.Join(", ", result)}]");
            return result;
        }
        catch (Exception ex)
        {
            await HandleCommunicationErrorAsync(ex);
            throw;
        }
    }

    #endregion

    #region 写入操作

    /// <summary>
    /// 写入单个线圈 (功能码 05)
    /// </summary>
    public async Task WriteSingleCoilAsync(ushort coilAddress, bool value)
    {
        if (!await EnsureConnectedAsync())
            throw new InvalidOperationException("未连接到从站");

        Log($"写入单个线圈: 地址={coilAddress}, 值={value}");
        try
        {
            await _master!.WriteSingleCoilAsync(SlaveId, coilAddress, value);
            Log("写入成功");
        }
        catch (Exception ex)
        {
            await HandleCommunicationErrorAsync(ex);
            throw;
        }
    }

    /// <summary>
    /// 写入单个寄存器 (功能码 06)
    /// </summary>
    public async Task WriteSingleRegisterAsync(ushort registerAddress, ushort value)
    {
        if (!await EnsureConnectedAsync())
            throw new InvalidOperationException("未连接到从站");

        Log($"写入单个寄存器: 地址={registerAddress}, 值={value}");
        try
        {
            await _master!.WriteSingleRegisterAsync(SlaveId, registerAddress, value);
            Log("写入成功");
        }
        catch (Exception ex)
        {
            await HandleCommunicationErrorAsync(ex);
            throw;
        }
    }

    /// <summary>
    /// 写入多个线圈 (功能码 15)
    /// </summary>
    public async Task WriteMultipleCoilsAsync(ushort startAddress, bool[] data)
    {
        if (!await EnsureConnectedAsync())
            throw new InvalidOperationException("未连接到从站");

        Log($"写入多个线圈: 起始地址={startAddress}, 数量={data.Length}");
        try
        {
            await _master!.WriteMultipleCoilsAsync(SlaveId, startAddress, data);
            Log("写入成功");
        }
        catch (Exception ex)
        {
            await HandleCommunicationErrorAsync(ex);
            throw;
        }
    }

    /// <summary>
    /// 写入多个寄存器 (功能码 16)
    /// </summary>
    public async Task WriteMultipleRegistersAsync(ushort startAddress, ushort[] data)
    {
        if (!await EnsureConnectedAsync())
            throw new InvalidOperationException("未连接到从站");

        Log($"写入多个寄存器: 起始地址={startAddress}, 数量={data.Length}");
        try
        {
            await _master!.WriteMultipleRegistersAsync(SlaveId, startAddress, data);
            Log("写入成功");
        }
        catch (Exception ex)
        {
            await HandleCommunicationErrorAsync(ex);
            throw;
        }
    }

    #endregion

    private async Task HandleCommunicationErrorAsync(Exception ex)
    {
        if (ex is IOException or SocketException)
        {
            Log($"通信异常: {ex.Message}");
            
            // 清理连接
            _master?.Dispose();
            _client?.Close();
            _master = null;
            _client = null;
            
            OnConnectionChanged?.Invoke(false);
            OnConnectionChangedAsync?.Invoke(false);

            if (AutoReconnect)
            {
                _ = StartReconnectAsync();
            }
        }
    }

    private void Log(string message)
    {
        var formattedMessage = $"[{DateTime.Now:HH:mm:ss}] {message}";
        OnLog?.Invoke(formattedMessage);
        OnLogAsync?.Invoke(formattedMessage);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        StopReconnect();
        Disconnect();
        GC.SuppressFinalize(this);
    }
}
