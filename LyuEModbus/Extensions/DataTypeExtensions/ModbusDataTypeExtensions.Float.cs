using LyuEModbus.Abstractions;
using LyuEModbus.Models;

namespace LyuEModbus.Extensions;

/// <summary>
/// Float 类型读写（32位，2个寄存器）
/// </summary>
public static partial class ModbusDataTypeExtensions
{
    #region Float (32-bit, 2 registers)

    /// <summary>
    /// 读取 Float（32位，占用2个寄存器）
    /// </summary>
    public static async Task<float?> ReadFloatAsync(
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
            return RegistersToFloat(registers, order);
        }, retryCount, onError, $"ReadFloat({address})");
    }

    /// <summary>
    /// 读取多个 Float
    /// </summary>
    public static async Task<float[]?> ReadFloatsAsync(
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
            var result = new float[count];
            for (int i = 0; i < count; i++)
            {
                var pair = new ushort[] { registers[i * 2], registers[i * 2 + 1] };
                result[i] = RegistersToFloat(pair, order);
            }
            return result;
        }, retryCount, onError, $"ReadFloats({address}, {count})");
    }

    /// <summary>
    /// 写入 Float
    /// </summary>
    public static async Task<bool> WriteFloatAsync(
        this IModbusMasterClient master,
        ushort address,
        float value,
        ByteOrder? byteOrder = null,
        Func<Exception, Task>? onError = null,
        int retryCount = 0)
    {
        var order = byteOrder ?? master.ByteOrder;
        return await ExecuteWithRetryBoolAsync(master, async () =>
        {
            var registers = FloatToRegisters(value, order);
            await master.WriteMultipleRegistersAsync(master.SlaveId, address, registers);
        }, retryCount, onError, $"WriteFloat({address}, {value})");
    }

    /// <summary>
    /// 写入多个 Float
    /// </summary>
    public static async Task<bool> WriteFloatsAsync(
        this IModbusMasterClient master,
        ushort address,
        float[] values,
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
                var pair = FloatToRegisters(values[i], order);
                registers[i * 2] = pair[0];
                registers[i * 2 + 1] = pair[1];
            }
            await master.WriteMultipleRegistersAsync(master.SlaveId, address, registers);
        }, retryCount, onError, $"WriteFloats({address}, {values.Length})");
    }

    #endregion
}
