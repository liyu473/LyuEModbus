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
        master.IpAddress = ipAddress;
        return master;
    }

    /// <summary>
    /// 设置目标地址和端口
    /// </summary>
    public static ModbusTcpMaster WithAddress(this ModbusTcpMaster master, string ipAddress, int port)
    {
        master.IpAddress = ipAddress;
        master.Port = port;
        return master;
    }

    /// <summary>
    /// 设置端口
    /// </summary>
    public static ModbusTcpMaster WithPort(this ModbusTcpMaster master, int port)
    {
        master.Port = port;
        return master;
    }

    /// <summary>
    /// 设置从站ID
    /// </summary>
    public static ModbusTcpMaster WithSlaveId(this ModbusTcpMaster master, byte slaveId)
    {
        master.SlaveId = slaveId;
        return master;
    }

    /// <summary>
    /// 设置超时
    /// </summary>
    public static ModbusTcpMaster WithTimeout(this ModbusTcpMaster master, int readTimeout, int writeTimeout)
    {
        master.ReadTimeout = readTimeout;
        master.WriteTimeout = writeTimeout;
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

    /// <summary>
    /// 启用自动重连
    /// </summary>
    public static ModbusTcpMaster WithAutoReconnect(this ModbusTcpMaster master, bool enabled = true)
    {
        master.AutoReconnect = enabled;
        return master;
    }

    /// <summary>
    /// 设置重连间隔
    /// </summary>
    public static ModbusTcpMaster WithReconnectInterval(this ModbusTcpMaster master, int intervalMs)
    {
        master.ReconnectInterval = intervalMs;
        return master;
    }

    /// <summary>
    /// 设置最大重连次数
    /// </summary>
    public static ModbusTcpMaster WithMaxReconnectAttempts(this ModbusTcpMaster master, int maxAttempts)
    {
        master.MaxReconnectAttempts = maxAttempts;
        return master;
    }

    /// <summary>
    /// 设置重连回调
    /// </summary>
    public static ModbusTcpMaster WithReconnecting(this ModbusTcpMaster master, Action<int> reconnectHandler)
    {
        master.OnReconnecting += reconnectHandler;
        return master;
    }
}
