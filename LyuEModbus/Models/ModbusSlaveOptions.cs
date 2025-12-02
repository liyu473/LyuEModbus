namespace LyuEModbus.Models;

/// <summary>
/// Modbus 从站配置
/// </summary>
public class ModbusSlaveOptions
{
    public string IpAddress { get; set; } = "0.0.0.0";
    public int Port { get; set; } = 502;
    public byte SlaveId { get; set; } = 1;
    public ushort InitHoldingRegisterCount { get; set; } = 100;
    public ushort InitCoilCount { get; set; } = 100;
    public int ChangeDetectionInterval { get; set; } = 100;
}
