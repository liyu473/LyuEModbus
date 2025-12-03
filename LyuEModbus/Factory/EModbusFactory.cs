using LyuEModbus.Abstractions;
using LyuEModbus.Core;
using LyuEModbus.Logging;
using LyuEModbus.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System.Collections.Concurrent;

namespace LyuEModbus.Factory;

/// <summary>
/// Modbus 工厂实现
/// </summary>
public class EModbusFactory : IEModbusFactory, IDisposable
{
    private readonly MicrosoftModbusLoggerFactory _loggerFactory;
    private readonly ConcurrentDictionary<string, IModbusMasterClient> _masters = new();
    private readonly ConcurrentDictionary<string, IModbusSlaveClient> _slaves = new();
    private bool _disposed;

    /// <summary>
    /// 默认主站配置（创建主站时自动应用）
    /// </summary>
    public ModbusMasterOptions DefaultMasterOptions { get; } = new();

    /// <summary>
    /// 默认从站配置（创建从站时自动应用）
    /// </summary>
    public ModbusSlaveOptions DefaultSlaveOptions { get; } = new();

    public EModbusFactory(ILoggerFactory loggerFactory)
    {
        ArgumentNullException.ThrowIfNull(loggerFactory);
        _loggerFactory = new MicrosoftModbusLoggerFactory(loggerFactory);
    }

    public EModbusFactory() : this(NullLoggerFactory.Instance) { }

    /// <summary>
    /// 配置默认主站选项
    /// </summary>
    public EModbusFactory ConfigureDefaultMaster(Action<ModbusMasterOptions> configure)
    {
        configure(DefaultMasterOptions);
        return this;
    }

    /// <summary>
    /// 配置默认从站选项
    /// </summary>
    public EModbusFactory ConfigureDefaultSlave(Action<ModbusSlaveOptions> configure)
    {
        configure(DefaultSlaveOptions);
        return this;
    }

    #region 工厂实现

    public IModbusMasterClient CreateTcpMaster(string name, ModbusMasterOptions? options = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("名称不能为空", nameof(name));
        if (_masters.ContainsKey(name))
            throw new InvalidOperationException($"名为 '{name}' 的主站已存在");

        var logger = _loggerFactory.CreateLogger($"Master:{name}");
        var mergedOptions = MergeOptions(DefaultMasterOptions, options);
        var master = new ModbusTcpMaster(name, mergedOptions, logger);

        if (!_masters.TryAdd(name, master))
        {
            master.Dispose();
            throw new InvalidOperationException($"名为 '{name}' 的主站已存在");
        }
        return master;
    }

    public IModbusSlaveClient CreateTcpSlave(string name, ModbusSlaveOptions? options = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("名称不能为空", nameof(name));
        if (_slaves.ContainsKey(name))
            throw new InvalidOperationException($"名为 '{name}' 的从站已存在");

        var logger = _loggerFactory.CreateLogger($"Slave:{name}");
        var mergedOptions = MergeOptions(DefaultSlaveOptions, options);
        var slave = new ModbusTcpSlave(name, mergedOptions, logger);

        if (!_slaves.TryAdd(name, slave))
        {
            slave.Dispose();
            throw new InvalidOperationException($"名为 '{name}' 的从站已存在");
        }
        return slave;
    }

    /// <summary>
    /// 合并配置（传入的 options 覆盖默认配置中的非空值）
    /// </summary>
    private static ModbusMasterOptions MergeOptions(ModbusMasterOptions defaults, ModbusMasterOptions? overrides)
    {
        if (overrides == null) return CloneOptions(defaults);

        return new ModbusMasterOptions
        {
            IpAddress = overrides.IpAddress ?? defaults.IpAddress,
            Port = overrides.Port ?? defaults.Port,
            SlaveId = overrides.SlaveId ?? defaults.SlaveId,
            ReadTimeout = overrides.ReadTimeout ?? defaults.ReadTimeout,
            WriteTimeout = overrides.WriteTimeout ?? defaults.WriteTimeout,
            AutoReconnect = overrides.AutoReconnect || defaults.AutoReconnect,
            ReconnectInterval = overrides.ReconnectInterval > 0 ? overrides.ReconnectInterval : defaults.ReconnectInterval,
            MaxReconnectAttempts = overrides.MaxReconnectAttempts > 0 ? overrides.MaxReconnectAttempts : defaults.MaxReconnectAttempts,
            EnableHeartbeat = overrides.EnableHeartbeat || defaults.EnableHeartbeat,
            HeartbeatInterval = overrides.HeartbeatInterval > 0 ? overrides.HeartbeatInterval : defaults.HeartbeatInterval,
            ByteOrder = overrides.ByteOrder != ByteOrder.ABCD ? overrides.ByteOrder : defaults.ByteOrder
        };
    }

    private static ModbusMasterOptions CloneOptions(ModbusMasterOptions source)
    {
        return new ModbusMasterOptions
        {
            IpAddress = source.IpAddress,
            Port = source.Port,
            SlaveId = source.SlaveId,
            ReadTimeout = source.ReadTimeout,
            WriteTimeout = source.WriteTimeout,
            AutoReconnect = source.AutoReconnect,
            ReconnectInterval = source.ReconnectInterval,
            MaxReconnectAttempts = source.MaxReconnectAttempts,
            EnableHeartbeat = source.EnableHeartbeat,
            HeartbeatInterval = source.HeartbeatInterval,
            ByteOrder = source.ByteOrder
        };
    }

    private static ModbusSlaveOptions MergeOptions(ModbusSlaveOptions defaults, ModbusSlaveOptions? overrides)
    {
        if (overrides == null) return CloneOptions(defaults);

        return new ModbusSlaveOptions
        {
            IpAddress = overrides.IpAddress ?? defaults.IpAddress,
            Port = overrides.Port ?? defaults.Port,
            SlaveId = overrides.SlaveId ?? defaults.SlaveId,
            InitHoldingRegisterCount = overrides.InitHoldingRegisterCount ?? defaults.InitHoldingRegisterCount,
            InitCoilCount = overrides.InitCoilCount ?? defaults.InitCoilCount,
            ChangeDetectionInterval = overrides.ChangeDetectionInterval > 0 ? overrides.ChangeDetectionInterval : defaults.ChangeDetectionInterval
        };
    }

    private static ModbusSlaveOptions CloneOptions(ModbusSlaveOptions source)
    {
        return new ModbusSlaveOptions
        {
            IpAddress = source.IpAddress,
            Port = source.Port,
            SlaveId = source.SlaveId,
            InitHoldingRegisterCount = source.InitHoldingRegisterCount,
            InitCoilCount = source.InitCoilCount,
            ChangeDetectionInterval = source.ChangeDetectionInterval
        };
    }

    public IModbusMasterClient? GetMaster(string name)
    {
        _masters.TryGetValue(name, out var master);
        return master;
    }

    public IModbusSlaveClient? GetSlave(string name)
    {
        _slaves.TryGetValue(name, out var slave);
        return slave;
    }

    /// <summary>
    /// 获取或创建 TCP 主站（如果已存在则返回现有实例）
    /// </summary>
    public IModbusMasterClient GetOrCreateTcpMaster(string name, ModbusMasterOptions? options = null)
    {
        if (_masters.TryGetValue(name, out var existing))
            return existing;
        return CreateTcpMaster(name, options);
    }

    /// <summary>
    /// 获取或创建 TCP 从站（如果已存在则返回现有实例）
    /// </summary>
    public IModbusSlaveClient GetOrCreateTcpSlave(string name, ModbusSlaveOptions? options = null)
    {
        if (_slaves.TryGetValue(name, out var existing))
            return existing;
        return CreateTcpSlave(name, options);
    }

    public IEnumerable<IModbusMasterClient> GetAllMasters() => _masters.Values;
    public IEnumerable<IModbusSlaveClient> GetAllSlaves() => _slaves.Values;

    public bool RemoveMaster(string name)
    {
        if (_masters.TryRemove(name, out var master))
        {
            master.Dispose();
            return true;
        }
        return false;
    }

    public bool RemoveSlave(string name)
    {
        if (_slaves.TryRemove(name, out var slave))
        {
            slave.Dispose();
            return true;
        }
        return false;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        foreach (var master in _masters.Values) master.Dispose();
        _masters.Clear();
        foreach (var slave in _slaves.Values) slave.Dispose();
        _slaves.Clear();

        GC.SuppressFinalize(this);
    }

    #endregion
}
