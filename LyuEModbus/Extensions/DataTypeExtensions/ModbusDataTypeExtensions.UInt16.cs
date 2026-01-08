using LyuEModbus.Abstractions;

namespace LyuEModbus.Extensions;

/// <summary>
/// UInt16 类型读写（16位无符号整数，1个寄存器）
/// </summary>
public static partial class ModbusDataTypeExtensions
{
    #region UInt16 (16-bit, 1 register)

    /// <summary>
    /// 读取 UInt16（16位无符号整数，占用1个寄存器）
    /// </summary>
    public static async Task<ushort?> ReadUInt16Async(
        this IModbusMasterClient master,
        ushort address,
        Func<Exception, Task>? onError = null,
        int retryCount = 0)
    {
        return await ExecuteWithRetryAsync(master, async () =>
        {
            var registers = await master.ReadHoldingRegistersAsync(master.SlaveId, address, 1);
            return registers[0];
        }, retryCount, onError, $"ReadUInt16({address})");
    }

    /// <summary>
    /// 读取多个 UInt16
    /// </summary>
    public static async Task<ushort[]?> ReadUInt16sAsync(
        this IModbusMasterClient master,
        ushort address,
        ushort count,
        Func<Exception, Task>? onError = null,
        int retryCount = 0)
    {
        return await ExecuteWithRetryRefAsync(master, async () =>
        {
            var registers = await master.ReadHoldingRegistersAsync(master.SlaveId, address, count);
            return registers;
        }, retryCount, onError, $"ReadUInt16s({address}, {count})");
    }

    /// <summary>
    /// 写入 UInt16
    /// </summary>
    public static async Task<bool> WriteUInt16Async(
        this IModbusMasterClient master,
        ushort address,
        ushort value,
        Func<Exception, Task>? onError = null,
        int retryCount = 0)
    {
        return await ExecuteWithRetryBoolAsync(master, async () =>
        {
            await master.WriteSingleRegisterAsync(master.SlaveId, address, value);
        }, retryCount, onError, $"WriteUInt16({address}, {value})");
    }

    /// <summary>
    /// 写入多个 UInt16
    /// </summary>
    public static async Task<bool> WriteUInt16sAsync(
        this IModbusMasterClient master,
        ushort address,
        ushort[] values,
        Func<Exception, Task>? onError = null,
        int retryCount = 0)
    {
        return await ExecuteWithRetryBoolAsync(master, async () =>
        {
            await master.WriteMultipleRegistersAsync(master.SlaveId, address, values);
        }, retryCount, onError, $"WriteUInt16s({address}, {values.Length})");
    }

    #endregion
}
