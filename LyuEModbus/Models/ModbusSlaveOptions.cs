namespace LyuEModbus.Models;

/// <summary>
/// Modbus 从站配置
/// </summary>
public class ModbusSlaveOptions
{
    public string? IpAddress { get; set; }
    public int? Port { get; set; }
    public byte? SlaveId { get; set; }
    public ushort? InitHoldingRegisterCount { get; set; }
    public ushort? InitCoilCount { get; set; }
    public int ChangeDetectionInterval { get; set; }
}
