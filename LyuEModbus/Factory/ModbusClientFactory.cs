using LyuEModbus.Abstractions;
using LyuEModbus.Core;
using LyuEModbus.Logging;
using LyuEModbus.Models;
using System.Collections.Concurrent;

namespace LyuEModbus.Factory;

/// <summary>
/// Modbus 工厂实现
/// </summary>
public class ModbusClientFactory(IModbusLoggerFactory loggerFactory) : IModbusFactory, IDisposable
{
    private readonly IModbusLoggerFactory _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
    private readonly ConcurrentDictionary<string, IModbusMasterClient> _masters = new();
    private readonly ConcurrentDictionary<string, IModbusSlaveClient> _slaves = new();
    private bool _disposed;

    public static ModbusClientFactory Default { get; } = new(new ConsoleModbusLoggerFactory());

    public ModbusClientFactory() : this(NullModbusLoggerFactory.Instance) { }

    #region 工厂实现

    public IModbusMasterClient CreateTcpMaster(string name, Action<ModbusMasterOptions> configure)
    {
        var options = new ModbusMasterOptions();
        configure(options);
        return CreateTcpMaster(name, options);
    }

    public IModbusMasterClient CreateTcpMaster(string name, ModbusMasterOptions options)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("名称不能为空", nameof(name));
        if (_masters.ContainsKey(name))
            throw new InvalidOperationException($"名为 '{name}' 的主站已存在");

        var logger = _loggerFactory.CreateLogger($"Master:{name}");
        var master = new ModbusTcpMaster(name, options, logger);

        if (!_masters.TryAdd(name, master))
        {
            master.Dispose();
            throw new InvalidOperationException($"名为 '{name}' 的主站已存在");
        }
        return master;
    }

    public IModbusSlaveClient CreateTcpSlave(string name, Action<ModbusSlaveOptions> configure)
    {
        var options = new ModbusSlaveOptions();
        configure(options);
        return CreateTcpSlave(name, options);
    }

    public IModbusSlaveClient CreateTcpSlave(string name, ModbusSlaveOptions options)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("名称不能为空", nameof(name));
        if (_slaves.ContainsKey(name))
            throw new InvalidOperationException($"名为 '{name}' 的从站已存在");

        var logger = _loggerFactory.CreateLogger($"Slave:{name}");
        var slave = new ModbusTcpSlave(name, options, logger);

        if (!_slaves.TryAdd(name, slave))
        {
            slave.Dispose();
            throw new InvalidOperationException($"名为 '{name}' 的从站已存在");
        }
        return slave;
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
