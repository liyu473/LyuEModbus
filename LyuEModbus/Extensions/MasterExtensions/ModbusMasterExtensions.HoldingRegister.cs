using LyuEModbus.Abstractions;

namespace LyuEModbus.Extensions;

/// <summary>
/// 保持寄存器（Holding Register）读写扩展方法 - 功能码 03/06/16
/// </summary>
public static partial class ModbusMasterExtensions
{
    #region 保持寄存器读取

    /// <summary>
    /// 读取单个保持寄存器
    /// </summary>
    public static Task<ushort?> ReadHoldingRegisterAsync(
        this IModbusMasterClient master,
        ushort address,
        Func<Exception, Task>? onError = null,
        int retryCount = 0)
    {
        return ExecuteWithRetryAsync(master, async () =>
        {
            var result = await master.ReadHoldingRegistersAsync(master.SlaveId, address, 1);
            return result[0];
        }, retryCount, onError, $"ReadHoldingRegister({address})");
    }

    /// <summary>
    /// 批量读取保持寄存器并返回数组
    /// </summary>
    public static Task<ushort[]?> ReadHoldingRegistersAsync(
        this IModbusMasterClient master,
        ushort startAddress,
        ushort count,
        Func<Exception, Task>? onError = null,
        int retryCount = 0)
    {
        return ExecuteWithRetryRefAsync(master, async () =>
        {
            return await master.ReadHoldingRegistersAsync(master.SlaveId, startAddress, count);
        }, retryCount, onError, $"ReadHoldingRegisters({startAddress}, {count})");
    }

    /// <summary>
    /// 批量读取保持寄存器并返回字典
    /// </summary>
    public static Task<Dictionary<ushort, ushort>?> ReadHoldingRegistersToDictAsync(
        this IModbusMasterClient master,
        ushort startAddress,
        ushort count,
        Func<Exception, Task>? onError = null,
        int retryCount = 0)
    {
        return ExecuteWithRetryRefAsync(master, async () =>
        {
            var result = await master.ReadHoldingRegistersAsync(master.SlaveId, startAddress, count);
            var dict = new Dictionary<ushort, ushort>();
            for (int i = 0; i < result.Length; i++)
                dict[(ushort)(startAddress + i)] = result[i];
            return dict;
        }, retryCount, onError, $"ReadHoldingRegistersToDict({startAddress}, {count})",
        d => string.Join(", ", d.Select(kv => $"{kv.Key}={kv.Value}")));
    }

    #endregion

    #region 保持寄存器写入

    /// <summary>
    /// 写入单个保持寄存器
    /// </summary>
    public static Task<bool> WriteRegisterAsync(
        this IModbusMasterClient master,
        ushort address,
        ushort value,
        Func<Exception, Task>? onError = null,
        int retryCount = 0)
    {
        return ExecuteWithRetryBoolAsync(master,
            () => master.WriteSingleRegisterAsync(master.SlaveId, address, value),
            retryCount, onError, $"WriteRegister({address}, {value})");
    }

    /// <summary>
    /// 批量写入保持寄存器
    /// </summary>
    public static Task<bool> WriteRegistersAsync(
        this IModbusMasterClient master,
        ushort startAddress,
        ushort[] values,
        Func<Exception, Task>? onError = null,
        int retryCount = 0)
    {
        return ExecuteWithRetryBoolAsync(master,
            () => master.WriteMultipleRegistersAsync(master.SlaveId, startAddress, values),
            retryCount, onError, $"WriteRegisters({startAddress}, {values.Length})");
    }

    /// <summary>
    /// 递增寄存器值
    /// </summary>
    public static Task<ushort?> IncrementRegisterAsync(
        this IModbusMasterClient master,
        ushort address,
        ushort increment = 1,
        Func<Exception, Task>? onError = null,
        int retryCount = 0)
    {
        return ExecuteWithRetryAsync(master, async () =>
        {
            var result = await master.ReadHoldingRegistersAsync(master.SlaveId, address, 1);
            var newValue = (ushort)(result[0] + increment);
            await master.WriteSingleRegisterAsync(master.SlaveId, address, newValue);
            return newValue;
        }, retryCount, onError, $"IncrementRegister({address}, +{increment})");
    }

    #endregion
}
