using LyuEModbus.Abstractions;
using LyuEModbus.Core;
using LyuEModbus.Models;

namespace LyuEModbus.Extensions;

/// <summary>
/// IModbusMasterClient 链式配置扩展方法
/// </summary>
public static class ModbusMasterClientExtensions
{
    /// <summary>
    /// 配置连接地址
    /// </summary>
    public static IModbusMasterClient WithEndpoint(this IModbusMasterClient master, string ipAddress, int port = 502)
    {
        if (master is ModbusTcpMaster tcpMaster)
            tcpMaster.ConfigureEndpoint(ipAddress, port);
        return master;
    }

    /// <summary>
    /// 配置从站ID
    /// </summary>
    public static IModbusMasterClient WithSlaveId(this IModbusMasterClient master, byte slaveId)
    {
        if (master is ModbusTcpMaster tcpMaster)
            tcpMaster.ConfigureSlaveId(slaveId);
        return master;
    }

    /// <summary>
    /// 配置超时时间
    /// </summary>
    public static IModbusMasterClient WithTimeout(this IModbusMasterClient master, int timeoutMs)
    {
        if (master is ModbusTcpMaster tcpMaster)
            tcpMaster.ConfigureTimeout(timeoutMs, timeoutMs);
        return master;
    }

    /// <summary>
    /// 配置读写超时时间
    /// </summary>
    public static IModbusMasterClient WithTimeout(this IModbusMasterClient master, int readTimeoutMs, int writeTimeoutMs)
    {
        if (master is ModbusTcpMaster tcpMaster)
            tcpMaster.ConfigureTimeout(readTimeoutMs, writeTimeoutMs);
        return master;
    }

    /// <summary>
    /// 订阅状态变化事件
    /// </summary>
    public static IModbusMasterClient OnStateChanged(this IModbusMasterClient master, Action<ModbusConnectionState> handler)
    {
        master.StateChanged += handler;
        return master;
    }

    /// <summary>
    /// 订阅重连事件并启用自动重连
    /// </summary>
    public static IModbusMasterClient OnReconnecting(this IModbusMasterClient master, Action<int> handler, int intervalMs = 5000, int maxAttempts = 0)
    {
        if (master is ModbusTcpMaster tcpMaster)
            tcpMaster.ConfigureReconnect(true, intervalMs, maxAttempts);
        master.Reconnecting += handler;
        return master;
    }

    /// <summary>
    /// 订阅心跳事件并启用心跳检测
    /// </summary>
    public static IModbusMasterClient OnHeartbeat(this IModbusMasterClient master, Action handler, int intervalMs = 5000)
    {
        if (master is ModbusTcpMaster tcpMaster)
            tcpMaster.ConfigureHeartbeat(true, intervalMs);
        master.Heartbeat += handler;
        return master;
    }
}
