using LyuEModbus.Abstractions;
using LyuEModbus.Models;

namespace LyuEModbus.Extensions;

/// <summary>
/// UInt32 类型读写（32位无符号整数，2个寄存器）
/// </summary>
public static partial class ModbusDataTypeExtensions
{
    #region UInt32 (32-bit, 2 registers)

    /// <summary>
    /// 读取 UInt32（32位无符号整数，占用2个寄存器）
    /// </summary>
    public static async Task<uint?> ReadUInt32Async(
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
            return RegistersToUInt32(registers, order);
        }, retryCount, onError, $"ReadUInt32({address})");
    }

    /// <summary>
    /// 读取多个 UInt32
    /// </summary>
    public static async Task<uint[]?> ReadUInt32sAsync(
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
            var result = new uint[count];
            for (int i = 0; i < count; i++)
            {
                var pair = new ushort[] { registers[i * 2], registers[i * 2 + 1] };
                result[i] = RegistersToUInt32(pair, order);
            }
            return result;
        }, retryCount, onError, $"ReadUInt32s({address}, {count})");
    }

    /// <summary>
    /// 写入 UInt32
    /// </summary>
    public static async Task<bool> WriteUInt32Async(
        this IModbusMasterClient master,
        ushort address,
        uint value,
        ByteOrder? byteOrder = null,
        Func<Exception, Task>? onError = null,
        int retryCount = 0)
    {
        var order = byteOrder ?? master.ByteOrder;
        return await ExecuteWithRetryBoolAsync(master, async () =>
        {
            var registers = UInt32ToRegisters(value, order);
            await master.WriteMultipleRegistersAsync(master.SlaveId, address, registers);
        }, retryCount, onError, $"WriteUInt32({address}, {value})");
    }

    /// <summary>
    /// 写入多个 UInt32
    /// </summary>
    public static async Task<bool> WriteUInt32sAsync(
        this IModbusMasterClient master,
        ushort address,
        uint[] values,
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
                var pair = UInt32ToRegisters(values[i], order);
                registers[i * 2] = pair[0];
                registers[i * 2 + 1] = pair[1];
            }
            await master.WriteMultipleRegistersAsync(master.SlaveId, address, registers);
        }, retryCount, onError, $"WriteUInt32s({address}, {values.Length})");
    }

    #endregion
}
