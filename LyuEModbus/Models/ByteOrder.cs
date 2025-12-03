namespace LyuEModbus.Models;

/// <summary>
/// Modbus 多寄存器数据的字节序
/// </summary>
public enum ByteOrder
{
    /// <summary>
    /// Big-Endian (AB CD) - 高字在前，高字节在前
    /// <para>常见：西门子 S7、Modicon、ABB</para>
    /// </summary>
    ABCD,

    /// <summary>
    /// Little-Endian (CD AB) - 低字在前，高字节在前
    /// <para>常见：三菱、欧姆龙、台达</para>
    /// </summary>
    CDAB,

    /// <summary>
    /// Mid-Big (BA DC) - 高字在前，低字节在前
    /// <para>常见：部分老设备</para>
    /// </summary>
    BADC,

    /// <summary>
    /// Mid-Little (DC BA) - 低字在前，低字节在前
    /// <para>少见</para>
    /// </summary>
    DCBA
}
