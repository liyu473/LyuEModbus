using LyuEModbus.Abstractions;
using LyuEModbus.Models;

namespace LyuEModbus.Extensions;

/// <summary>
/// Int64 类型读写（64位有符号整数，4个寄存器）
/// </summary>
public static partial class ModbusDataTypeExtensions
{
    #region Int64 (64-bit, 4 registers)

    /// <summary>
    /// 读取 Int64（64位有符号整数，占用4个寄存器）
    /// </summary>
    public static async Task<long?> ReadInt64Async(
        this IModbusMasterClient master,
        ushort address,
        ByteOrder? byteOrder = null,
        Func<Exception, Task>? onError = null,
        int retryCount = 0)
    {
        var order = byteOrder ?? master.ByteOrder;
        return await ExecuteWithRetryAsync(master, async () =>
        {
            var registers = await master.ReadHoldingRegistersAsync(master.SlaveId, address, 4);
            return RegistersToInt64(registers, order);
        }, retryCount, onError, $"ReadInt64({address})");
    }

    /// <summary>
    /// 读取多个 Int64
    /// </summary>
    public static async Task<long[]?> ReadInt64sAsync(
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
            var registers = await master.ReadHoldingRegistersAsync(master.SlaveId, address, (ushort)(count * 4));
            var result = new long[count];
            for (int i = 0; i < count; i++)
            {
                var quad = new ushort[] { registers[i * 4], registers[i * 4 + 1], registers[i * 4 + 2], registers[i * 4 + 3] };
                result[i] = RegistersToInt64(quad, order);
            }
            return result;
        }, retryCount, onError, $"ReadInt64s({address}, {count})");
    }


    /// <summary>
    /// 写入 Int64
    /// </summary>
    public static async Task<bool> WriteInt64Async(
        this IModbusMasterClient master,
        ushort address,
        long value,
        ByteOrder? byteOrder = null,
        Func<Exception, Task>? onError = null,
        int retryCount = 0)
    {
        var order = byteOrder ?? master.ByteOrder;
        return await ExecuteWithRetryBoolAsync(master, async () =>
        {
            var registers = Int64ToRegisters(value, order);
            await master.WriteMultipleRegistersAsync(master.SlaveId, address, registers);
        }, retryCount, onError, $"WriteInt64({address}, {value})");
    }

    /// <summary>
    /// 写入多个 Int64
    /// </summary>
    public static async Task<bool> WriteInt64sAsync(
        this IModbusMasterClient master,
        ushort address,
        long[] values,
        ByteOrder? byteOrder = null,
        Func<Exception, Task>? onError = null,
        int retryCount = 0)
    {
        var order = byteOrder ?? master.ByteOrder;
        return await ExecuteWithRetryBoolAsync(master, async () =>
        {
            var registers = new ushort[values.Length * 4];
            for (int i = 0; i < values.Length; i++)
            {
                var quad = Int64ToRegisters(values[i], order);
                registers[i * 4] = quad[0];
                registers[i * 4 + 1] = quad[1];
                registers[i * 4 + 2] = quad[2];
                registers[i * 4 + 3] = quad[3];
            }
            await master.WriteMultipleRegistersAsync(master.SlaveId, address, registers);
        }, retryCount, onError, $"WriteInt64s({address}, {values.Length})");
    }

    #endregion
}
