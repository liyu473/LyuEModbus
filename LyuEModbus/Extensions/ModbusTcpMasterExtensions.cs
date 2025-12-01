using LyuEModbus.Services;

namespace LyuEModbus.Extensions;

/// <summary>
/// ModbusTcpMaster 链式调用扩展
/// </summary>
public static class ModbusTcpMasterExtensions
{
    /// <summary>
    /// 设置目标地址
    /// </summary>
    public static ModbusTcpMaster WithAddress(this ModbusTcpMaster master, string ipAddress)
    {
        typeof(ModbusTcpMaster).GetProperty("IpAddress")?.SetValue(master, ipAddress);
        return master;
    }

    /// <summary>
    /// 设置目标地址和端口
    /// </summary>
    public static ModbusTcpMaster WithAddress(this ModbusTcpMaster master, string ipAddress, int port)
    {
        typeof(ModbusTcpMaster).GetProperty("IpAddress")?.SetValue(master, ipAddress);
        typeof(ModbusTcpMaster).GetProperty("Port")?.SetValue(master, port);
        return master;
    }

    /// <summary>
    /// 设置端口
    /// </summary>
    public static ModbusTcpMaster WithPort(this ModbusTcpMaster master, int port)
    {
        typeof(ModbusTcpMaster).GetProperty("Port")?.SetValue(master, port);
        return master;
    }

    /// <summary>
    /// 设置从站ID
    /// </summary>
    public static ModbusTcpMaster WithSlaveId(this ModbusTcpMaster master, byte slaveId)
    {
        typeof(ModbusTcpMaster).GetProperty("SlaveId")?.SetValue(master, slaveId);
        return master;
    }

    /// <summary>
    /// 设置超时
    /// </summary>
    public static ModbusTcpMaster WithTimeout(this ModbusTcpMaster master, int readTimeout, int writeTimeout)
    {
        typeof(ModbusTcpMaster).GetProperty("ReadTimeout")?.SetValue(master, readTimeout);
        typeof(ModbusTcpMaster).GetProperty("WriteTimeout")?.SetValue(master, writeTimeout);
        return master;
    }

    /// <summary>
    /// 设置超时（读写使用相同值）
    /// </summary>
    public static ModbusTcpMaster WithTimeout(this ModbusTcpMaster master, int timeout)
    {
        return master.WithTimeout(timeout, timeout);
    }

    /// <summary>
    /// 设置日志回调
    /// </summary>
    public static ModbusTcpMaster WithLog(this ModbusTcpMaster master, Action<string> logHandler)
    {
        master.OnLog += logHandler;
        return master;
    }

    /// <summary>
    /// 设置连接状态变化回调
    /// </summary>
    public static ModbusTcpMaster WithConnectionChanged(this ModbusTcpMaster master, Action<bool> connectionHandler)
    {
        master.OnConnectionChanged += connectionHandler;
        return master;
    }
}
