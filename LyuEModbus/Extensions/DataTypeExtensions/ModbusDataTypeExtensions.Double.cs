using LyuEModbus.Abstractions;
using LyuEModbus.Models;

namespace LyuEModbus.Extensions;

/// <summary>
/// Double 类型读写（64位，4个寄存器）
/// </summary>
public static partial class ModbusDataTypeExtensions
{
    #region Double (64-bit, 4 registers)

    /// <summary>
    /// 读取 Double（64位，占用4个寄存器）
    /// </summary>
    public static async Task<double?> ReadDoubleAsync(
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
            return RegistersToDouble(registers, order);
        }, retryCount, onError, $"ReadDouble({address})");
    }

    /// <summary>
    /// 读取多个 Double
    /// </summary>
    public static async Task<double[]?> ReadDoublesAsync(
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
            var result = new double[count];
            for (int i = 0; i < count; i++)
            {
                var quad = new ushort[] { registers[i * 4], registers[i * 4 + 1], registers[i * 4 + 2], registers[i * 4 + 3] };
                result[i] = RegistersToDouble(quad, order);
            }
            return result;
        }, retryCount, onError, $"ReadDoubles({address}, {count})");
    }


    /// <summary>
    /// 写入 Double
    /// </summary>
    public static async Task<bool> WriteDoubleAsync(
        this IModbusMasterClient master,
        ushort address,
        double value,
        ByteOrder? byteOrder = null,
        Func<Exception, Task>? onError = null,
        int retryCount = 0)
    {
        var order = byteOrder ?? master.ByteOrder;
        return await ExecuteWithRetryBoolAsync(master, async () =>
        {
            var registers = DoubleToRegisters(value, order);
            await master.WriteMultipleRegistersAsync(master.SlaveId, address, registers);
        }, retryCount, onError, $"WriteDouble({address}, {value})");
    }

    /// <summary>
    /// 写入多个 Double
    /// </summary>
    public static async Task<bool> WriteDoublesAsync(
        this IModbusMasterClient master,
        ushort address,
        double[] values,
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
                var quad = DoubleToRegisters(values[i], order);
                registers[i * 4] = quad[0];
                registers[i * 4 + 1] = quad[1];
                registers[i * 4 + 2] = quad[2];
                registers[i * 4 + 3] = quad[3];
            }
            await master.WriteMultipleRegistersAsync(master.SlaveId, address, registers);
        }, retryCount, onError, $"WriteDoubles({address}, {values.Length})");
    }

    #endregion
}
