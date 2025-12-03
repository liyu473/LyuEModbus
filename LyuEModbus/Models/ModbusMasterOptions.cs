namespace LyuEModbus.Models;

/// <summary>
/// Modbus 主站配置
/// </summary>
public class ModbusMasterOptions
{
    public string? IpAddress { get; set; }
    public int? Port { get; set; }
    public byte? SlaveId { get; set; }
    public int? ReadTimeout { get; set; }
    public int? WriteTimeout { get; set; }
    public bool AutoReconnect { get; set; }
    public int ReconnectInterval { get; set; }
    public int MaxReconnectAttempts { get; set; }
    public bool EnableHeartbeat { get; set; }
    public int HeartbeatInterval { get; set; }

    /// <summary>
    /// 默认字节序（用于 Float、Int32、Double 等多寄存器数据类型）
    /// </summary>
    public ByteOrder ByteOrder { get; set; } = ByteOrder.ABCD;
}
