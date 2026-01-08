using LyuEModbus.Abstractions;

namespace LyuEModbus.Extensions;

/// <summary>
/// Int16 类型读写（16位有符号整数，1个寄存器）
/// </summary>
public static partial class ModbusDataTypeExtensions
{
    #region Int16 (16-bit, 1 register)

    /// <summary>
    /// 读取 Int16（16位有符号整数，占用1个寄存器）
    /// </summary>
    public static async Task<short?> ReadInt16Async(
        this IModbusMasterClient master,
        ushort address,
        Func<Exception, Task>? onError = null,
        int retryCount = 0)
    {
        return await ExecuteWithRetryAsync(master, async () =>
        {
            var registers = await master.ReadHoldingRegistersAsync(master.SlaveId, address, 1);
            return (short)registers[0];
        }, retryCount, onError, $"ReadInt16({address})");
    }

    /// <summary>
    /// 读取多个 Int16
    /// </summary>
    public static async Task<short[]?> ReadInt16sAsync(
        this IModbusMasterClient master,
        ushort address,
        ushort count,
        Func<Exception, Task>? onError = null,
        int retryCount = 0)
    {
        return await ExecuteWithRetryRefAsync(master, async () =>
        {
            var registers = await master.ReadHoldingRegistersAsync(master.SlaveId, address, count);
            var result = new short[count];
            for (int i = 0; i < count; i++)
            {
                result[i] = (short)registers[i];
            }
            return result;
        }, retryCount, onError, $"ReadInt16s({address}, {count})");
    }

    /// <summary>
    /// 写入 Int16
    /// </summary>
    public static async Task<bool> WriteInt16Async(
        this IModbusMasterClient master,
        ushort address,
        short value,
        Func<Exception, Task>? onError = null,
        int retryCount = 0)
    {
        return await ExecuteWithRetryBoolAsync(master, async () =>
        {
            await master.WriteSingleRegisterAsync(master.SlaveId, address, (ushort)value);
        }, retryCount, onError, $"WriteInt16({address}, {value})");
    }

    /// <summary>
    /// 写入多个 Int16
    /// </summary>
    public static async Task<bool> WriteInt16sAsync(
        this IModbusMasterClient master,
        ushort address,
        short[] values,
        Func<Exception, Task>? onError = null,
        int retryCount = 0)
    {
        return await ExecuteWithRetryBoolAsync(master, async () =>
        {
            var registers = new ushort[values.Length];
            for (int i = 0; i < values.Length; i++)
            {
                registers[i] = (ushort)values[i];
            }
            await master.WriteMultipleRegistersAsync(master.SlaveId, address, registers);
        }, retryCount, onError, $"WriteInt16s({address}, {values.Length})");
    }

    #endregion
}
