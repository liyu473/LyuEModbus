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
    /// <summary>
    /// 数据变化检测间隔（毫秒），用于触发 HoldingRegisterWritten/CoilWritten 事件
    /// <para>默认值: 100ms</para>
    /// </summary>
    public int ChangeDetectionInterval { get; set; } = 100;
}
