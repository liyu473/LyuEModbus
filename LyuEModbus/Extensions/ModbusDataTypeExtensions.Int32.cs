using LyuEModbus.Abstractions;
using LyuEModbus.Models;

namespace LyuEModbus.Extensions;

/// <summary>
/// Int32 类型读写（32位有符号整数，2个寄存器）
/// </summary>
public static partial class ModbusDataTypeExtensions
{
    #region Int32 (32-bit, 2 registers)

    /// <summary>
    /// 读取 Int32（32位有符号整数，占用2个寄存器）
    /// </summary>
    public static async Task<int?> ReadInt32Async(
        this IModbusMasterClient master,
        ushort address,
        ByteOrder? byteOrder = null,
        Func<Exception, Task>? onError = null,
        int retryCount = 0)
    {
        var order = byteOrder ?? master.ByteOrder;
        return await ExecuteWithRetryAsync(master, async () =>
        {
            var registers = await master.ReadHoldingRegistersAsync(master.SlaveId, address, 2);
            return RegistersToInt32(registers, order);
        }, retryCount, onError, $"ReadInt32({address})");
    }

    /// <summary>
    /// 读取多个 Int32
    /// </summary>
    public static async Task<int[]?> ReadInt32sAsync(
        this IModbusMasterClient master,
        ushort address,
        int count,
        ByteOrder? byteOrder = null,
        Func<Exception, Task>? onError = null,
        int retryCount = 0)
    {
        var order = byteOrder ?? master.ByteOrder;
        return await ExecuteWithRetryRefAsync(master, async () =>
        {
            var registers = await master.ReadHoldingRegistersAsync(master.SlaveId, address, (ushort)(count * 2));
            var result = new int[count];
            for (int i = 0; i < count; i++)
            {
                var pair = new ushort[] { registers[i * 2], registers[i * 2 + 1] };
                result[i] = RegistersToInt32(pair, order);
            }
            return result;
        }, retryCount, onError, $"ReadInt32s({address}, {count})");
    }

    /// <summary>
    /// 写入 Int32
    /// </summary>
    public static async Task<bool> WriteInt32Async(
        this IModbusMasterClient master,
        ushort address,
        int value,
        ByteOrder? byteOrder = null,
        Func<Exception, Task>? onError = null,
        int retryCount = 0)
    {
        var order = byteOrder ?? master.ByteOrder;
        return await ExecuteWithRetryBoolAsync(master, async () =>
        {
            var registers = Int32ToRegisters(value, order);
            await master.WriteMultipleRegistersAsync(master.SlaveId, address, registers);
        }, retryCount, onError, $"WriteInt32({address}, {value})");
    }

    /// <summary>
    /// 写入多个 Int32
    /// </summary>
    public static async Task<bool> WriteInt32sAsync(
        this IModbusMasterClient master,
        ushort address,
        int[] values,
        ByteOrder? byteOrder = null,
        Func<Exception, Task>? onError = null,
        int retryCount = 0)
    {
        var order = byteOrder ?? master.ByteOrder;
        return await ExecuteWithRetryBoolAsync(master, async () =>
        {
            var registers = new ushort[values.Length * 2];
            for (int i = 0; i < values.Length; i++)
            {
                var pair = Int32ToRegisters(values[i], order);
                registers[i * 2] = pair[0];
                registers[i * 2 + 1] = pair[1];
            }
            await master.WriteMultipleRegistersAsync(master.SlaveId, address, registers);
        }, retryCount, onError, $"WriteInt32s({address}, {values.Length})");
    }

    #endregion
}
