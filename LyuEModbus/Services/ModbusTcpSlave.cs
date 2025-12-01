using NModbus;
using System.Net.Sockets;

namespace LyuEModbus.Services;

/// <summary>
/// Modbus TCP 从站封装
/// </summary>
public class ModbusTcpSlave : IDisposable
{
    private TcpListener? _listener;
    private IModbusSlaveNetwork? _slaveNetwork;
    private IModbusSlave? _slave;
    private CancellationTokenSource? _cts;
    private bool _disposed;

    /// <summary>
    /// 从站ID
    /// </summary>
    public byte SlaveId { get; internal set; } = 1;

    /// <summary>
    /// 监听地址
    /// </summary>
    public string IpAddress { get; internal set; } = "0.0.0.0";

    /// <summary>
    /// 监听端口
    /// </summary>
    public int Port { get; internal set; } = 502;

    /// <summary>
    /// 是否正在运行
    /// </summary>
    public bool IsRunning { get; private set; }

    /// <summary>
    /// 从站数据存储
    /// </summary>
    public ISlaveDataStore? DataStore => _slave?.DataStore;

    /// <summary>
    /// 日志事件
    /// </summary>
    public event Action<string>? OnLog;

    /// <summary>
    /// 状态变化事件
    /// </summary>
    public event Action<bool>? OnStatusChanged;

    /// <summary>
    /// 创建 Modbus TCP 从站
    /// </summary>
    /// <param name="ipAddress">监听地址，默认 0.0.0.0</param>
    /// <param name="port">端口号，默认 502</param>
    /// <param name="slaveId">从站ID，默认 1</param>
    public ModbusTcpSlave(string ipAddress = "0.0.0.0", int port = 502, byte slaveId = 1)
    {
        IpAddress = ipAddress;
        Port = port;
        SlaveId = slaveId;
    }

    /// <summary>
    /// 创建从站构建器
    /// </summary>
    public static ModbusTcpSlave Create() => new();

    /// <summary>
    /// 启动从站（支持链式调用）
    /// </summary>
    public async Task<ModbusTcpSlave> StartAsync()
    {
        if (IsRunning)
        {
            Log("从站已在运行中");
            return this;
        }

        try
        {
            var ip = System.Net.IPAddress.Parse(IpAddress);
            _listener = new TcpListener(ip, Port);
            _listener.Start();

            var factory = new ModbusFactory();
            _slaveNetwork = factory.CreateSlaveNetwork(_listener);

            _slave = factory.CreateSlave(SlaveId);
            _slaveNetwork.AddSlave(_slave);

            _cts = new CancellationTokenSource();

            // 后台运行监听
            _ = Task.Run(async () =>
            {
                try
                {
                    await _slaveNetwork.ListenAsync(_cts.Token);
                }
                catch (OperationCanceledException)
                {
                    // 正常取消
                }
                catch (Exception ex)
                {
                    Log($"监听异常: {ex.Message}");
                }
            });

            IsRunning = true;
            OnStatusChanged?.Invoke(true);
            Log($"从站已启动 - {IpAddress}:{Port}, SlaveId: {SlaveId}");
        }
        catch (Exception ex)
        {
            Log($"启动失败: {ex.Message}");
            throw;
        }
        return this;
    }

    /// <summary>
    /// 停止从站
    /// </summary>
    public void Stop()
    {
        if (!IsRunning)
        {
            Log("从站未运行");
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

            IsRunning = false;
            OnStatusChanged?.Invoke(false);
            Log("从站已停止");
        }
        catch (Exception ex)
        {
            Log($"停止失败: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// 设置线圈值
    /// </summary>
    public void SetCoil(ushort address, bool value)
    {
        var dataStore = _slave?.DataStore;
        dataStore?.CoilDiscretes.WritePoints(address, new[] { value });
    }

    /// <summary>
    /// 设置保持寄存器值
    /// </summary>
    public void SetHoldingRegister(ushort address, ushort value)
    {
        var dataStore = _slave?.DataStore;
        dataStore?.HoldingRegisters.WritePoints(address, new[] { value });
    }

    /// <summary>
    /// 批量设置保持寄存器
    /// </summary>
    public void SetHoldingRegisters(ushort startAddress, ushort[] values)
    {
        var dataStore = _slave?.DataStore;
        dataStore?.HoldingRegisters.WritePoints(startAddress, values);
    }

    /// <summary>
    /// 读取保持寄存器值
    /// </summary>
    public ushort[]? ReadHoldingRegisters(ushort startAddress, ushort count)
    {
        var dataStore = _slave?.DataStore;
        return dataStore?.HoldingRegisters.ReadPoints(startAddress, count);
    }

    private void Log(string message)
    {
        OnLog?.Invoke($"[{DateTime.Now:HH:mm:ss}] {message}");
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        Stop();
        GC.SuppressFinalize(this);
    }
}
