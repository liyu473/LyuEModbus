using LyuEModbus.Abstractions;

namespace LyuEModbus.Extensions;

/// <summary>
/// Boolean 类型读写（保持寄存器，1个寄存器 = 1个布尔值，0=false, 非0=true）
/// </summary>
public static partial class ModbusDataTypeExtensions
{
    #region Boolean (16-bit, 1 register)

    /// <summary>
    /// 读取 Boolean（从保持寄存器，0=false, 非0=true）
    /// </summary>
    public static async Task<bool?> ReadBooleanAsync(
        this IModbusMasterClient master,
        ushort address,
        Func<Exception, Task>? onError = null,
        int retryCount = 0)
    {
        return await ExecuteWithRetryAsync(master, async () =>
        {
            var registers = await master.ReadHoldingRegistersAsync(master.SlaveId, address, 1);
            return registers[0] != 0;
        }, retryCount, onError, $"ReadBoolean({address})");
    }

    /// <summary>
    /// 读取多个 Boolean（从保持寄存器）
    /// </summary>
    public static async Task<bool[]?> ReadBooleansAsync(
        this IModbusMasterClient master,
        ushort address,
        int count,
        Func<Exception, Task>? onError = null,
        int retryCount = 0)
    {
        return await ExecuteWithRetryRefAsync(master, async () =>
        {
            var registers = await master.ReadHoldingRegistersAsync(master.SlaveId, address, (ushort)count);
            var result = new bool[count];
            for (int i = 0; i < count; i++)
                result[i] = registers[i] != 0;
            return result;
        }, retryCount, onError, $"ReadBooleans({address}, {count})");
    }

    /// <summary>
    /// 写入 Boolean（到保持寄存器，true=1, false=0）
    /// </summary>
    public static async Task<bool> WriteBooleanAsync(
        this IModbusMasterClient master,
        ushort address,
        bool value,
        Func<Exception, Task>? onError = null,
        int retryCount = 0)
    {
        return await ExecuteWithRetryBoolAsync(master, async () =>
        {
            await master.WriteSingleRegisterAsync(master.SlaveId, address, (ushort)(value ? 1 : 0));
        }, retryCount, onError, $"WriteBoolean({address}, {value})");
    }

    /// <summary>
    /// 写入多个 Boolean（到保持寄存器）
    /// </summary>
    public static async Task<bool> WriteBooleansAsync(
        this IModbusMasterClient master,
        ushort address,
        bool[] values,
        Func<Exception, Task>? onError = null,
        int retryCount = 0)
    {
        return await ExecuteWithRetryBoolAsync(master, async () =>
        {
            var registers = new ushort[values.Length];
            for (int i = 0; i < values.Length; i++)
                registers[i] = (ushort)(values[i] ? 1 : 0);
            await master.WriteMultipleRegistersAsync(master.SlaveId, address, registers);
        }, retryCount, onError, $"WriteBooleans({address}, {values.Length})");
    }

    #endregion
}
