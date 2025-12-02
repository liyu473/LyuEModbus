using LyuEModbus.Abstractions;
using LyuEModbus.Core;

namespace LyuEModbus.Extensions;

/// <summary>
/// IModbusSlaveClient 链式配置扩展方法
/// </summary>
public static class ModbusSlaveClientExtensions
{
    /// <summary>
    /// 配置监听地址
    /// </summary>
    public static IModbusSlaveClient WithEndpoint(this IModbusSlaveClient slave, string ipAddress, int port = 502)
    {
        if (slave is ModbusTcpSlave tcpSlave)
            tcpSlave.ConfigureEndpoint(ipAddress, port);
        return slave;
    }

    /// <summary>
    /// 配置从站ID
    /// </summary>
    public static IModbusSlaveClient WithSlaveId(this IModbusSlaveClient slave, byte slaveId)
    {
        if (slave is ModbusTcpSlave tcpSlave)
            tcpSlave.ConfigureSlaveId(slaveId);
        return slave;
    }

    /// <summary>
    /// 配置数据存储区大小
    /// </summary>
    public static IModbusSlaveClient WithDataStore(this IModbusSlaveClient slave, ushort holdingRegisterCount, ushort coilCount)
    {
        if (slave is ModbusTcpSlave tcpSlave)
            tcpSlave.ConfigureDataStore(holdingRegisterCount, coilCount);
        return slave;
    }

    /// <summary>
    /// 配置数据变化检测间隔
    /// <para>用于触发 HoldingRegisterWritten/CoilWritten 事件的轮询频率</para>
    /// </summary>
    /// <param name="slave">从站实例</param>
    /// <param name="intervalMs">检测间隔（毫秒），最小 100ms，默认 100ms</param>
    public static IModbusSlaveClient WithChangeDetectionInterval(this IModbusSlaveClient slave, int intervalMs)
    {
        if (slave is ModbusTcpSlave tcpSlave)
            tcpSlave.ConfigureChangeDetectionInterval(intervalMs);
        return slave;
    }

    /// <summary>
    /// 订阅运行状态变化事件（支持异步回调）
    /// </summary>
    public static IModbusSlaveClient OnRunningChanged(this IModbusSlaveClient slave, Func<bool, Task> handler)
    {
        slave.RunningChanged += handler;
        return slave;
    }

    /// <summary>
    /// 订阅保持寄存器写入事件（支持异步回调）
    /// </summary>
    public static IModbusSlaveClient OnHoldingRegisterWritten(this IModbusSlaveClient slave, Func<ushort, ushort, ushort, Task> handler)
    {
        slave.HoldingRegisterWritten += handler;
        return slave;
    }

    /// <summary>
    /// 订阅线圈写入事件（支持异步回调）
    /// </summary>
    public static IModbusSlaveClient OnCoilWritten(this IModbusSlaveClient slave, Func<ushort, bool, Task> handler)
    {
        slave.CoilWritten += handler;
        return slave;
    }

    /// <summary>
    /// 订阅客户端连接事件（支持异步回调）
    /// </summary>
    public static IModbusSlaveClient OnClientConnected(this IModbusSlaveClient slave, Func<string, Task> handler)
    {
        slave.ClientConnected += handler;
        return slave;
    }

    /// <summary>
    /// 订阅客户端断开事件（支持异步回调）
    /// </summary>
    public static IModbusSlaveClient OnClientDisconnected(this IModbusSlaveClient slave, Func<string, Task> handler)
    {
        slave.ClientDisconnected += handler;
        return slave;
    }
}
