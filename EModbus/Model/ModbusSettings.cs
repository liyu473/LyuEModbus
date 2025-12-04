using CommunityToolkit.Mvvm.ComponentModel;
using LyuEModbus.Models;

namespace EModbus.Model;

/// <summary>
/// Modbus 配置
/// </summary>
public partial class ModbusSettings : ObservableObject
{
    /// <summary>
    /// 主站配置
    /// </summary>
    [ObservableProperty]
    public partial MasterSettings Master { get; set; } = new();

    /// <summary>
    /// 从站配置
    /// </summary>
    [ObservableProperty]
    public partial SlaveSettings Slave { get; set; } = new();
}

/// <summary>
/// 主站配置
/// </summary>
public partial class MasterSettings : ObservableObject
{
    /// <summary>
    /// 从站IP地址（主站要连接的目标）
    /// </summary>
    [ObservableProperty]
    public partial string IpAddress { get; set; } = "127.0.0.1";

    /// <summary>
    /// 端口号
    /// </summary>
    [ObservableProperty]
    public partial int Port { get; set; } = 502;

    /// <summary>
    /// 从站ID
    /// </summary>
    [ObservableProperty]
    public partial byte SlaveId { get; set; } = 1;

    /// <summary>
    /// 字节序设置
    /// </summary>
    [ObservableProperty]
    public partial ByteOrder ByteOrder { get; set; } = ByteOrder.ABCD;

    /// <summary>
    /// 读取超时（毫秒）
    /// </summary>
    [ObservableProperty]
    public partial int ReadTimeout { get; set; } = 3000;

    /// <summary>
    /// 写入超时（毫秒）
    /// </summary>
    [ObservableProperty]
    public partial int WriteTimeout { get; set; } = 3000;
}

/// <summary>
/// 从站配置
/// </summary>
public partial class SlaveSettings : ObservableObject
{
    /// <summary>
    /// 监听IP地址
    /// </summary>
    [ObservableProperty]
    public partial string IpAddress { get; set; } = "0.0.0.0";

    /// <summary>
    /// 监听端口号
    /// </summary>
    [ObservableProperty]
    public partial int Port { get; set; } = 502;

    /// <summary>
    /// 从站ID
    /// </summary>
    [ObservableProperty]
    public partial byte SlaveId { get; set; } = 1;
}
