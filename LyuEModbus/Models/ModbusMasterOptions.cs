namespace LyuEModbus.Models;

/// <summary>
/// Modbus 主站配置
/// </summary>
public class ModbusMasterOptions
{
    public string IpAddress { get; set; } = "127.0.0.1";
    public int Port { get; set; } = 502;
    public byte SlaveId { get; set; } = 1;
    public int ReadTimeout { get; set; } = 3000;
    public int WriteTimeout { get; set; } = 3000;
    public bool AutoReconnect { get; set; } = false;
    public int ReconnectInterval { get; set; } = 5000;
    public int MaxReconnectAttempts { get; set; } = 0;
    public bool EnableHeartbeat { get; set; } = false;
    public int HeartbeatInterval { get; set; } = 5000;
}
