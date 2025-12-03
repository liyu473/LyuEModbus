using LyuEModbus.Abstractions;
using LyuEModbus.Models;

namespace LyuEModbus.Extensions;

/// <summary>
/// UInt64 类型读写（64位无符号整数，4个寄存器）
/// </summary>
public static partial class ModbusDataTypeExtensions
{
    #region UInt64 (64-bit, 4 registers)

    /// <summary>
    /// 读取 UInt64（64位无符号整数，占用4个寄存器）
    /// </summary>
    public static async Task<ulong?> ReadUInt64Async(
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
            return RegistersToUInt64(registers, order);
        }, retryCount, onError, $"ReadUInt64({address})");
    }

    /// <summary>
    /// 读取多个 UInt64
    /// </summary>
    public static async Task<ulong[]?> ReadUInt64sAsync(
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
            var result = new ulong[count];
            for (int i = 0; i < count; i++)
            {
                var quad = new ushort[] { registers[i * 4], registers[i * 4 + 1], registers[i * 4 + 2], registers[i * 4 + 3] };
                result[i] = RegistersToUInt64(quad, order);
            }
            return result;
        }, retryCount, onError, $"ReadUInt64s({address}, {count})");
    }


    /// <summary>
    /// 写入 UInt64
    /// </summary>
    public static async Task<bool> WriteUInt64Async(
        this IModbusMasterClient master,
        ushort address,
        ulong value,
        ByteOrder? byteOrder = null,
        Func<Exception, Task>? onError = null,
        int retryCount = 0)
    {
        var order = byteOrder ?? master.ByteOrder;
        return await ExecuteWithRetryBoolAsync(master, async () =>
        {
            var registers = UInt64ToRegisters(value, order);
            await master.WriteMultipleRegistersAsync(master.SlaveId, address, registers);
        }, retryCount, onError, $"WriteUInt64({address}, {value})");
    }

    /// <summary>
    /// 写入多个 UInt64
    /// </summary>
    public static async Task<bool> WriteUInt64sAsync(
        this IModbusMasterClient master,
        ushort address,
        ulong[] values,
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
                var quad = UInt64ToRegisters(values[i], order);
                registers[i * 4] = quad[0];
                registers[i * 4 + 1] = quad[1];
                registers[i * 4 + 2] = quad[2];
                registers[i * 4 + 3] = quad[3];
            }
            await master.WriteMultipleRegistersAsync(master.SlaveId, address, registers);
        }, retryCount, onError, $"WriteUInt64s({address}, {values.Length})");
    }

    #endregion
}