using LyuEModbus.Abstractions;
using LyuEModbus.Core;
using System.Collections.Concurrent;

namespace LyuEModbus.Factory;

/// <summary>
/// Modbus 工厂实现
/// </summary>
public class ModbusClientFactory : IModbusFactory, IDisposable
{
    private readonly IModbusLoggerFactory _loggerFactory;
    private readonly ConcurrentDictionary<string, IModbusMaster> _masters = new();
    private readonly ConcurrentDictionary<string,IModbusSlave> _slaves = new();
    private bool _disposed;

    /// <summary>
    /// 默认静态实例（使用控制台日志）
    /// </summary>
    public static ModbusClientFactory Default { get; } = new(new ConsoleModbusLoggerFactory());

    /// <summary>
    /// 创建工厂实例
    /// </summary>
    public ModbusClientFactory(IModbusLoggerFactory loggerFactory)
    {
        _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
    }

    /// <summary>
    /// 创建工厂实例（无日志）
    /// </summary>
    public ModbusClientFactory() : this(NullModbusLoggerFactory.Instance)
    {
    }

    /// <inheritdoc />
    public Abstractions.IModbusMaster CreateTcpMaster(string name, Action<ModbusMasterOptions> configure)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("名称不能为空", nameof(name));

        var options = new ModbusMasterOptions();
        configure(options);
        return CreateTcpMaster(name, options);
    }

    /// <inheritdoc />
    public IModbusMaster CreateTcpMaster(string name, ModbusMasterOptions options)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("名称不能为空", nameof(name));
        if (options == null)
            throw new ArgumentNullException(nameof(options));

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

    /// <inheritdoc />
    public IModbusSlave CreateTcpSlave(string name, Action<ModbusSlaveOptions> configure)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("名称不能为空", nameof(name));

        var options = new ModbusSlaveOptions();
        configure(options);
        return CreateTcpSlave(name, options);
    }

    /// <inheritdoc />
    public IModbusSlave CreateTcpSlave(string name, ModbusSlaveOptions options)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("名称不能为空", nameof(name));
        if (options == null)
            throw new ArgumentNullException(nameof(options));

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

    /// <inheritdoc />
    public IModbusMaster? GetMaster(string name)
    {
        _masters.TryGetValue(name, out var master);
        return master;
    }

    /// <inheritdoc />
    public IModbusSlave? GetSlave(string name)
    {
        _slaves.TryGetValue(name, out var slave);
        return slave;
    }

    /// <inheritdoc />
    public IEnumerable<IModbusMaster> GetAllMasters() => _masters.Values;

    /// <inheritdoc />
    public IEnumerable<IModbusSlave> GetAllSlaves() => _slaves.Values;

    /// <inheritdoc />
    public bool RemoveMaster(string name)
    {
        if (_masters.TryRemove(name, out var master))
        {
            master.Dispose();
            return true;
        }
        return false;
    }

    /// <inheritdoc />
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

        foreach (var master in _masters.Values)
            master.Dispose();
        _masters.Clear();

        foreach (var slave in _slaves.Values)
            slave.Dispose();
        _slaves.Clear();

        GC.SuppressFinalize(this);
    }
}
