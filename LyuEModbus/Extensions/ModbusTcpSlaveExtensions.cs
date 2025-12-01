using LyuEModbus.Services;

namespace LyuEModbus.Extensions;

/// <summary>
/// ModbusTcpSlave 链式调用扩展
/// </summary>
public static class ModbusTcpSlaveExtensions
{
    /// <summary>
    /// 设置监听地址
    /// </summary>
    public static ModbusTcpSlave WithAddress(this ModbusTcpSlave slave, string ipAddress)
    {
        slave.IpAddress = ipAddress;
        return slave;
    }

    /// <summary>
    /// 设置监听地址和端口
    /// </summary>
    public static ModbusTcpSlave WithAddress(this ModbusTcpSlave slave, string ipAddress, int port)
    {
        slave.IpAddress = ipAddress;
        slave.Port = port;
        return slave;
    }

    /// <summary>
    /// 设置端口
    /// </summary>
    public static ModbusTcpSlave WithPort(this ModbusTcpSlave slave, int port)
    {
        slave.Port = port;
        return slave;
    }

    /// <summary>
    /// 设置从站ID
    /// </summary>
    public static ModbusTcpSlave WithSlaveId(this ModbusTcpSlave slave, byte slaveId)
    {
        slave.SlaveId = slaveId;
        return slave;
    }

    /// <summary>
    /// 设置日志回调
    /// </summary>
    public static ModbusTcpSlave WithLog(this ModbusTcpSlave slave, Action<string> logHandler)
    {
        slave.OnLog += logHandler;
        return slave;
    }

    /// <summary>
    /// 设置状态变化回调
    /// </summary>
    public static ModbusTcpSlave WithStatusChanged(this ModbusTcpSlave slave, Action<bool> statusHandler)
    {
        slave.OnStatusChanged += statusHandler;
        return slave;
    }
}
