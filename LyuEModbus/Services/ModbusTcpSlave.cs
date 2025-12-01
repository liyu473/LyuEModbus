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
    private ushort[]? _lastHoldingValues;
    private bool[]? _lastCoilValues;

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
    /// 初始化保持寄存器数量
    /// </summary>
    public ushort InitHoldingRegisterCount { get; internal set; } = 100;

    /// <summary>
    /// 初始化线圈数量
    /// </summary>
    public ushort InitCoilCount { get; internal set; } = 100;

    /// <summary>
    /// 变化检测间隔（毫秒）
    /// </summary>
    public int ChangeDetectionInterval { get; internal set; } = 100;

    /// <summary>
    /// 从站数据存储
    /// </summary>
    public ISlaveDataStore? DataStore => _slave?.DataStore;

    /// <summary>
    /// 日志事件
    /// </summary>
    public event Action<string>? OnLog;

    /// <summary>
    /// 异步日志事件
    /// </summary>
    public event Func<string, Task>? OnLogAsync;

    /// <summary>
    /// 状态变化事件
    /// </summary>
    public event Action<bool>? OnStatusChanged;

    /// <summary>
    /// 异步状态变化事件
    /// </summary>
    public event Func<bool, Task>? OnStatusChangedAsync;

    /// <summary>
    /// 寄存器值被修改事件 (地址, 旧值, 新值)
    /// </summary>
    public event Action<ushort, ushort, ushort>? OnHoldingRegisterWritten;

    /// <summary>
    /// 异步寄存器值被修改事件 (地址, 旧值, 新值)
    /// </summary>
    public event Func<ushort, ushort, ushort, Task>? OnHoldingRegisterWrittenAsync;

    /// <summary>
    /// 线圈值被修改事件 (地址, 值)
    /// </summary>
    public event Action<ushort, bool>? OnCoilWritten;

    /// <summary>
    /// 异步线圈值被修改事件 (地址, 值)
    /// </summary>
    public event Func<ushort, bool, Task>? OnCoilWrittenAsync;

    /// <summary>
    /// 创建 Modbus TCP 从站
    /// </summary>
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
    /// 启动从站
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

            // 初始化模拟数据
            InitializeSimulationData();

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

            // 监控数据变化
            _ = Task.Run(MonitorDataChangesAsync);

            IsRunning = true;
            OnStatusChanged?.Invoke(true);
            if (OnStatusChangedAsync != null)
            {
                await OnStatusChangedAsync.Invoke(true);
            }
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
    /// 初始化模拟数据
    /// </summary>
    private void InitializeSimulationData()
    {
        if (_slave?.DataStore == null) return;

        // 初始化保持寄存器
        _lastHoldingValues = new ushort[InitHoldingRegisterCount];
        for (int i = 0; i < _lastHoldingValues.Length; i++)
        {
            _lastHoldingValues[i] = (ushort)(i * 10);
        }
        _slave.DataStore.HoldingRegisters.WritePoints(0, _lastHoldingValues);

        // 初始化线圈
        _lastCoilValues = new bool[InitCoilCount];
        for (int i = 0; i < _lastCoilValues.Length; i++)
        {
            _lastCoilValues[i] = i % 2 == 0;
        }
        _slave.DataStore.CoilDiscretes.WritePoints(0, _lastCoilValues);

        Log($"已初始化模拟数据: {InitHoldingRegisterCount}个保持寄存器, {InitCoilCount}个线圈");
    }

    /// <summary>
    /// 监控数据变化（轮询方式）
    /// </summary>
    private async Task MonitorDataChangesAsync()
    {
        while (_cts != null && !_cts.Token.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(ChangeDetectionInterval, _cts.Token);
                await DetectChangesAsync();
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                Log($"监控异常: {ex.Message}");
            }
        }
    }

    private async Task DetectChangesAsync()
    {
        if (_slave?.DataStore == null || _lastHoldingValues == null || _lastCoilValues == null) return;

        // 检测保持寄存器变化
        var currentHolding = _slave.DataStore.HoldingRegisters.ReadPoints(0, (ushort)_lastHoldingValues.Length);
        for (int i = 0; i < currentHolding.Length; i++)
        {
            if (currentHolding[i] != _lastHoldingValues[i])
            {
                var address = (ushort)i;
                var oldValue = _lastHoldingValues[i];
                var newValue = currentHolding[i];
                
                Log($"保持寄存器被修改: 地址={address}, 旧值={oldValue}, 新值={newValue}");
                OnHoldingRegisterWritten?.Invoke(address, oldValue, newValue);
                if (OnHoldingRegisterWrittenAsync != null)
                {
                    await OnHoldingRegisterWrittenAsync.Invoke(address, oldValue, newValue);
                }
                
                _lastHoldingValues[i] = newValue;
            }
        }

        // 检测线圈变化
        var currentCoils = _slave.DataStore.CoilDiscretes.ReadPoints(0, (ushort)_lastCoilValues.Length);
        for (int i = 0; i < currentCoils.Length; i++)
        {
            if (currentCoils[i] != _lastCoilValues[i])
            {
                var address = (ushort)i;
                var value = currentCoils[i];
                
                Log($"线圈被修改: 地址={address}, 值={value}");
                OnCoilWritten?.Invoke(address, value);
                if (OnCoilWrittenAsync != null)
                {
                    await OnCoilWrittenAsync.Invoke(address, value);
                }
                
                _lastCoilValues[i] = value;
            }
        }
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
            _lastHoldingValues = null;
            _lastCoilValues = null;

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
        _slave?.DataStore?.CoilDiscretes.WritePoints(address, new[] { value });
        if (_lastCoilValues != null && address < _lastCoilValues.Length)
        {
            _lastCoilValues[address] = value;
        }
    }

    /// <summary>
    /// 设置保持寄存器值
    /// </summary>
    public void SetHoldingRegister(ushort address, ushort value)
    {
        _slave?.DataStore?.HoldingRegisters.WritePoints(address, new[] { value });
        if (_lastHoldingValues != null && address < _lastHoldingValues.Length)
        {
            _lastHoldingValues[address] = value;
        }
    }

    /// <summary>
    /// 批量设置保持寄存器
    /// </summary>
    public void SetHoldingRegisters(ushort startAddress, ushort[] values)
    {
        _slave?.DataStore?.HoldingRegisters.WritePoints(startAddress, values);
        if (_lastHoldingValues != null)
        {
            for (int i = 0; i < values.Length && startAddress + i < _lastHoldingValues.Length; i++)
            {
                _lastHoldingValues[startAddress + i] = values[i];
            }
        }
    }

    /// <summary>
    /// 读取保持寄存器值
    /// </summary>
    public ushort[]? ReadHoldingRegisters(ushort startAddress, ushort count)
    {
        return _slave?.DataStore?.HoldingRegisters.ReadPoints(startAddress, count);
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

        Stop();
        GC.SuppressFinalize(this);
    }
}
