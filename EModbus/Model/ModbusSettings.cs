using CommunityToolkit.Mvvm.ComponentModel;

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
    private MasterSettings master = new();

    /// <summary>
    /// 从站配置
    /// </summary>
    [ObservableProperty]
    private SlaveSettings slave = new();
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
    private string ipAddress = "127.0.0.1";

    /// <summary>
    /// 端口号
    /// </summary>
    [ObservableProperty]
    private int port = 502;

    /// <summary>
    /// 从站ID
    /// </summary>
    [ObservableProperty]
    private byte slaveId = 1;

    /// <summary>
    /// 读取超时（毫秒）
    /// </summary>
    [ObservableProperty]
    private int readTimeout = 3000;

    /// <summary>
    /// 写入超时（毫秒）
    /// </summary>
    [ObservableProperty]
    private int writeTimeout = 3000;
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
    private string ipAddress = "0.0.0.0";

    /// <summary>
    /// 监听端口号
    /// </summary>
    [ObservableProperty]
    private int port = 502;

    /// <summary>
    /// 从站ID
    /// </summary>
    [ObservableProperty]
    private byte slaveId = 1;
}
