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
    public ModbusMasterOptions DefaultMasterOptions { get; } = new() { Name = "__default__" };

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

    #region 主站管理

    /// <inheritdoc />
    public IModbusMasterClient CreateTcpMaster(string name, ModbusMasterOptions? options = null)
    {
        ValidateName(name, nameof(name));
        EnsureMasterNotExists(name);

        var logger = _loggerFactory.CreateLogger($"Master:{name}");
        var mergedOptions = DefaultMasterOptions.MergeWith(options, name);
        var master = new ModbusTcpMaster(name, mergedOptions, logger);

        if (!_masters.TryAdd(name, master))
        {
            master.Dispose();
            throw new InvalidOperationException($"名为 '{name}' 的主站已存在");
        }
        return master;
    }

    /// <inheritdoc />
    public IModbusMasterClient? GetMaster(string name)
    {
        _masters.TryGetValue(name, out var master);
        return master;
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

    /// <inheritdoc />
    public IEnumerable<IModbusMasterClient> GetAllMasters() => _masters.Values;

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

    #endregion

    #region 从站管理

    /// <inheritdoc />
    public IModbusSlaveClient CreateTcpSlave(string name, ModbusSlaveOptions? options = null)
    {
        ValidateName(name, nameof(name));
        EnsureSlaveNotExists(name);

        var logger = _loggerFactory.CreateLogger($"Slave:{name}");
        var mergedOptions = DefaultSlaveOptions.MergeWith(options);
        var slave = new ModbusTcpSlave(name, mergedOptions, logger);

        if (!_slaves.TryAdd(name, slave))
        {
            slave.Dispose();
            throw new InvalidOperationException($"名为 '{name}' 的从站已存在");
        }
        return slave;
    }

    /// <inheritdoc />
    public IModbusSlaveClient? GetSlave(string name)
    {
        _slaves.TryGetValue(name, out var slave);
        return slave;
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

    /// <inheritdoc />
    public IEnumerable<IModbusSlaveClient> GetAllSlaves() => _slaves.Values;

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

    #endregion

    #region Internal 方法

    internal static void ValidateName(string name, string paramName)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("名称不能为空", paramName);
    }

    internal void EnsureMasterNotExists(string name)
    {
        if (_masters.ContainsKey(name))
            throw new InvalidOperationException($"名为 '{name}' 的主站已存在");
    }

    internal void EnsureSlaveNotExists(string name)
    {
        if (_slaves.ContainsKey(name))
            throw new InvalidOperationException($"名为 '{name}' 的从站已存在");
    }

    #endregion

    #region IDisposable

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
