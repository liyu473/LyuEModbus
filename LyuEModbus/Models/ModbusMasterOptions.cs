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
}
