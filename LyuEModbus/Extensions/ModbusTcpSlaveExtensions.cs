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
    /// 设置异步日志回调
    /// </summary>
    public static ModbusTcpSlave WithLogAsync(this ModbusTcpSlave slave, Func<string, Task> logHandler)
    {
        slave.OnLogAsync += logHandler;
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

    /// <summary>
    /// 设置异步状态变化回调
    /// </summary>
    public static ModbusTcpSlave WithStatusChangedAsync(this ModbusTcpSlave slave, Func<bool, Task> statusHandler)
    {
        slave.OnStatusChangedAsync += statusHandler;
        return slave;
    }

    /// <summary>
    /// 设置初始化保持寄存器数量
    /// </summary>
    public static ModbusTcpSlave WithInitHoldingRegisters(this ModbusTcpSlave slave, ushort count)
    {
        slave.InitHoldingRegisterCount = count;
        return slave;
    }

    /// <summary>
    /// 设置初始化线圈数量
    /// </summary>
    public static ModbusTcpSlave WithInitCoils(this ModbusTcpSlave slave, ushort count)
    {
        slave.InitCoilCount = count;
        return slave;
    }

    /// <summary>
    /// 设置保持寄存器写入回调
    /// </summary>
    public static ModbusTcpSlave WithHoldingRegisterWritten(this ModbusTcpSlave slave, Action<ushort, ushort, ushort> handler)
    {
        slave.OnHoldingRegisterWritten += handler;
        return slave;
    }

    /// <summary>
    /// 设置异步保持寄存器写入回调
    /// </summary>
    public static ModbusTcpSlave WithHoldingRegisterWrittenAsync(this ModbusTcpSlave slave, Func<ushort, ushort, ushort, Task> handler)
    {
        slave.OnHoldingRegisterWrittenAsync += handler;
        return slave;
    }

    /// <summary>
    /// 设置线圈写入回调
    /// </summary>
    public static ModbusTcpSlave WithCoilWritten(this ModbusTcpSlave slave, Action<ushort, bool> handler)
    {
        slave.OnCoilWritten += handler;
        return slave;
    }

    /// <summary>
    /// 设置异步线圈写入回调
    /// </summary>
    public static ModbusTcpSlave WithCoilWrittenAsync(this ModbusTcpSlave slave, Func<ushort, bool, Task> handler)
    {
        slave.OnCoilWrittenAsync += handler;
        return slave;
    }

    /// <summary>
    /// 设置客户端连接回调
    /// </summary>
    public static ModbusTcpSlave WithClientConnected(this ModbusTcpSlave slave, Action<string> handler)
    {
        slave.OnClientConnected += handler;
        return slave;
    }

    /// <summary>
    /// 设置异步客户端连接回调
    /// </summary>
    public static ModbusTcpSlave WithClientConnectedAsync(this ModbusTcpSlave slave, Func<string, Task> handler)
    {
        slave.OnClientConnectedAsync += handler;
        return slave;
    }

    /// <summary>
    /// 设置客户端断开回调
    /// </summary>
    public static ModbusTcpSlave WithClientDisconnected(this ModbusTcpSlave slave, Action<string> handler)
    {
        slave.OnClientDisconnected += handler;
        return slave;
    }

    /// <summary>
    /// 设置异步客户端断开回调
    /// </summary>
    public static ModbusTcpSlave WithClientDisconnectedAsync(this ModbusTcpSlave slave, Func<string, Task> handler)
    {
        slave.OnClientDisconnectedAsync += handler;
        return slave;
    }
}
