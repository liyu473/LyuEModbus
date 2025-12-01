namespace EModbus.Models;

/// <summary>
/// Modbus 配置
/// </summary>
public class ModbusSettings
{
    /// <summary>
    /// 主站配置
    /// </summary>
    public MasterSettings Master { get; set; } = new();

    /// <summary>
    /// 从站配置
    /// </summary>
    public SlaveSettings Slave { get; set; } = new();
}

/// <summary>
/// 主站配置
/// </summary>
public class MasterSettings
{
    /// <summary>
    /// 从站IP地址（主站要连接的目标）
    /// </summary>
    public string IpAddress { get; set; } = "127.0.0.1";

    /// <summary>
    /// 端口号
    /// </summary>
    public int Port { get; set; } = 502;

    /// <summary>
    /// 从站ID
    /// </summary>
    public byte SlaveId { get; set; } = 1;

    /// <summary>
    /// 读取超时（毫秒）
    /// </summary>
    public int ReadTimeout { get; set; } = 3000;

    /// <summary>
    /// 写入超时（毫秒）
    /// </summary>
    public int WriteTimeout { get; set; } = 3000;
}

/// <summary>
/// 从站配置
/// </summary>
public class SlaveSettings
{
    /// <summary>
    /// 监听IP地址
    /// </summary>
    public string IpAddress { get; set; } = "0.0.0.0";

    /// <summary>
    /// 监听端口号
    /// </summary>
    public int Port { get; set; } = 502;

    /// <summary>
    /// 从站ID
    /// </summary>
    public byte SlaveId { get; set; } = 1;
}
