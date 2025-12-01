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

    /// <summary>
    /// 从站IP地址
    /// </summary>
    public string IpAddress { get; private set; } = "127.0.0.1";

    /// <summary>
    /// 端口号
    /// </summary>
    public int Port { get; private set; } = 502;

    /// <summary>
    /// 从站ID
    /// </summary>
    public byte SlaveId { get; private set; } = 1;

    /// <summary>
    /// 读取超时（毫秒）
    /// </summary>
    public int ReadTimeout { get; private set; } = 3000;

    /// <summary>
    /// 写入超时（毫秒）
    /// </summary>
    public int WriteTimeout { get; private set; } = 3000;

    /// <summary>
    /// 是否已连接
    /// </summary>
    public bool IsConnected => _client?.Connected ?? false;

    /// <summary>
    /// 日志事件
    /// </summary>
    public event Action<string>? OnLog;

    /// <summary>
    /// 连接状态变化事件
    /// </summary>
    public event Action<bool>? OnConnectionChanged;

    private ModbusTcpMaster() { }

    /// <summary>
    /// 创建 Modbus TCP 主站
    /// </summary>
    public ModbusTcpMaster(string ipAddress, int port, byte slaveId, int readTimeout = 3000, int writeTimeout = 3000)
    {
        IpAddress = ipAddress;
        Port = port;
        SlaveId = slaveId;
        ReadTimeout = readTimeout;
        WriteTimeout = writeTimeout;
    }

    #region Fluent Builder

    /// <summary>
    /// 创建主站构建器
    /// </summary>
    public static ModbusTcpMaster Create() => new();

    /// <summary>
    /// 设置目标地址
    /// </summary>
    public ModbusTcpMaster WithAddress(string ipAddress)
    {
        IpAddress = ipAddress;
        return this;
    }

    /// <summary>
    /// 设置目标地址和端口
    /// </summary>
    public ModbusTcpMaster WithAddress(string ipAddress, int port)
    {
        IpAddress = ipAddress;
        Port = port;
        return this;
    }

    /// <summary>
    /// 设置端口
    /// </summary>
    public ModbusTcpMaster WithPort(int port)
    {
        Port = port;
        return this;
    }

    /// <summary>
    /// 设置从站ID
    /// </summary>
    public ModbusTcpMaster WithSlaveId(byte slaveId)
    {
        SlaveId = slaveId;
        return this;
    }

    /// <summary>
    /// 设置超时
    /// </summary>
    public ModbusTcpMaster WithTimeout(int readTimeout, int writeTimeout)
    {
        ReadTimeout = readTimeout;
        WriteTimeout = writeTimeout;
        return this;
    }

    /// <summary>
    /// 设置超时（读写使用相同值）
    /// </summary>
    public ModbusTcpMaster WithTimeout(int timeout)
    {
        ReadTimeout = timeout;
        WriteTimeout = timeout;
        return this;
    }

    /// <summary>
    /// 设置日志回调
    /// </summary>
    public ModbusTcpMaster WithLog(Action<string> logHandler)
    {
        OnLog += logHandler;
        return this;
    }

    /// <summary>
    /// 设置连接状态变化回调
    /// </summary>
    public ModbusTcpMaster WithConnectionChanged(Action<bool> connectionHandler)
    {
        OnConnectionChanged += connectionHandler;
        return this;
    }

    #endregion

    /// <summary>
    /// 连接到从站（支持链式调用）
    /// </summary>
    public async Task<ModbusTcpMaster> ConnectAsync()
    {
        if (IsConnected)
        {
            Log("已连接");
            return this;
        }

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

            OnConnectionChanged?.Invoke(true);
            Log($"已连接到 {IpAddress}:{Port}");
        }
        catch (Exception ex)
        {
            Log($"连接失败: {ex.Message}");
            throw;
        }
        return this;
    }

    /// <summary>
    /// 断开连接
    /// </summary>
    public void Disconnect()
    {
        if (!IsConnected)
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
            Log("已断开连接");
        }
        catch (Exception ex)
        {
            Log($"断开失败: {ex.Message}");
            throw;
        }
    }

    #region 读取操作

    /// <summary>
    /// 读取线圈 (功能码 01)
    /// </summary>
    public async Task<bool[]> ReadCoilsAsync(ushort startAddress, ushort numberOfPoints)
    {
        EnsureConnected();
        Log($"读取线圈: 起始地址={startAddress}, 数量={numberOfPoints}");
        var result = await _master!.ReadCoilsAsync(SlaveId, startAddress, numberOfPoints);
        Log($"读取结果: [{string.Join(", ", result)}]");
        return result;
    }

    /// <summary>
    /// 读取离散输入 (功能码 02)
    /// </summary>
    public async Task<bool[]> ReadInputsAsync(ushort startAddress, ushort numberOfPoints)
    {
        EnsureConnected();
        Log($"读取离散输入: 起始地址={startAddress}, 数量={numberOfPoints}");
        var result = await _master!.ReadInputsAsync(SlaveId, startAddress, numberOfPoints);
        Log($"读取结果: [{string.Join(", ", result)}]");
        return result;
    }

    /// <summary>
    /// 读取保持寄存器 (功能码 03)
    /// </summary>
    public async Task<ushort[]> ReadHoldingRegistersAsync(ushort startAddress, ushort numberOfPoints)
    {
        EnsureConnected();
        Log($"读取保持寄存器: 起始地址={startAddress}, 数量={numberOfPoints}");
        var result = await _master!.ReadHoldingRegistersAsync(SlaveId, startAddress, numberOfPoints);
        Log($"读取结果: [{string.Join(", ", result)}]");
        return result;
    }

    /// <summary>
    /// 读取输入寄存器 (功能码 04)
    /// </summary>
    public async Task<ushort[]> ReadInputRegistersAsync(ushort startAddress, ushort numberOfPoints)
    {
        EnsureConnected();
        Log($"读取输入寄存器: 起始地址={startAddress}, 数量={numberOfPoints}");
        var result = await _master!.ReadInputRegistersAsync(SlaveId, startAddress, numberOfPoints);
        Log($"读取结果: [{string.Join(", ", result)}]");
        return result;
    }

    #endregion

    #region 写入操作

    /// <summary>
    /// 写入单个线圈 (功能码 05)
    /// </summary>
    public async Task WriteSingleCoilAsync(ushort coilAddress, bool value)
    {
        EnsureConnected();
        Log($"写入单个线圈: 地址={coilAddress}, 值={value}");
        await _master!.WriteSingleCoilAsync(SlaveId, coilAddress, value);
        Log("写入成功");
    }

    /// <summary>
    /// 写入单个寄存器 (功能码 06)
    /// </summary>
    public async Task WriteSingleRegisterAsync(ushort registerAddress, ushort value)
    {
        EnsureConnected();
        Log($"写入单个寄存器: 地址={registerAddress}, 值={value}");
        await _master!.WriteSingleRegisterAsync(SlaveId, registerAddress, value);
        Log("写入成功");
    }

    /// <summary>
    /// 写入多个线圈 (功能码 15)
    /// </summary>
    public async Task WriteMultipleCoilsAsync(ushort startAddress, bool[] data)
    {
        EnsureConnected();
        Log($"写入多个线圈: 起始地址={startAddress}, 数量={data.Length}");
        await _master!.WriteMultipleCoilsAsync(SlaveId, startAddress, data);
        Log("写入成功");
    }

    /// <summary>
    /// 写入多个寄存器 (功能码 16)
    /// </summary>
    public async Task WriteMultipleRegistersAsync(ushort startAddress, ushort[] data)
    {
        EnsureConnected();
        Log($"写入多个寄存器: 起始地址={startAddress}, 数量={data.Length}");
        await _master!.WriteMultipleRegistersAsync(SlaveId, startAddress, data);
        Log("写入成功");
    }

    #endregion

    private void EnsureConnected()
    {
        if (!IsConnected || _master == null)
        {
            throw new InvalidOperationException("未连接到从站，请先调用 ConnectAsync()");
        }
    }

    private void Log(string message)
    {
        OnLog?.Invoke($"[{DateTime.Now:HH:mm:ss}] {message}");
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        Disconnect();
        GC.SuppressFinalize(this);
    }
}
