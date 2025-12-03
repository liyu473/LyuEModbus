using LyuEModbus.Abstractions;

namespace LyuEModbus.Extensions;

/// <summary>
/// 线圈（Coil）读写扩展方法 - 功能码 01/05/15
/// </summary>
public static partial class ModbusMasterExtensions
{
    #region 线圈读取

    /// <summary>
    /// 读取单个线圈
    /// </summary>
    public static Task<bool?> ReadCoilAsync(
        this IModbusMasterClient master,
        ushort address,
        Func<Exception, Task>? onError = null,
        int retryCount = 0)
    {
        return ExecuteWithRetryAsync(master, async () =>
        {
            var result = await master.ReadCoilsAsync(master.SlaveId, address, 1);
            return result[0];
        }, retryCount, onError, $"ReadCoil({address})");
    }

    /// <summary>
    /// 批量读取线圈并返回数组
    /// </summary>
    public static Task<bool[]?> ReadCoilsAsync(
        this IModbusMasterClient master,
        ushort startAddress,
        ushort count,
        Func<Exception, Task>? onError = null,
        int retryCount = 0)
    {
        return ExecuteWithRetryRefAsync(master, async () =>
        {
            return await master.ReadCoilsAsync(master.SlaveId, startAddress, count);
        }, retryCount, onError, $"ReadCoils({startAddress}, {count})");
    }

    /// <summary>
    /// 批量读取线圈并返回字典
    /// </summary>
    public static Task<Dictionary<ushort, bool>?> ReadCoilsToDictAsync(
        this IModbusMasterClient master,
        ushort startAddress,
        ushort count,
        Func<Exception, Task>? onError = null,
        int retryCount = 0)
    {
        return ExecuteWithRetryRefAsync(master, async () =>
        {
            var result = await master.ReadCoilsAsync(master.SlaveId, startAddress, count);
            var dict = new Dictionary<ushort, bool>();
            for (int i = 0; i < result.Length; i++)
                dict[(ushort)(startAddress + i)] = result[i];
            return dict;
        }, retryCount, onError, $"ReadCoilsToDict({startAddress}, {count})",
        d => string.Join(", ", d.Select(kv => $"{kv.Key}={kv.Value}")));
    }

    #endregion

    #region 线圈写入

    /// <summary>
    /// 写入单个线圈
    /// </summary>
    public static Task<bool> WriteCoilAsync(
        this IModbusMasterClient master,
        ushort address,
        bool value,
        Func<Exception, Task>? onError = null,
        int retryCount = 0)
    {
        return ExecuteWithRetryBoolAsync(master,
            () => master.WriteSingleCoilAsync(master.SlaveId, address, value),
            retryCount, onError, $"WriteCoil({address}, {value})");
    }

    /// <summary>
    /// 批量写入线圈
    /// </summary>
    public static Task<bool> WriteCoilsAsync(
        this IModbusMasterClient master,
        ushort startAddress,
        bool[] values,
        Func<Exception, Task>? onError = null,
        int retryCount = 0)
    {
        return ExecuteWithRetryBoolAsync(master,
            () => master.WriteMultipleCoilsAsync(master.SlaveId, startAddress, values),
            retryCount, onError, $"WriteCoils({startAddress}, {values.Length})");
    }

    /// <summary>
    /// 切换线圈状态
    /// </summary>
    public static Task<bool?> ToggleCoilAsync(
        this IModbusMasterClient master,
        ushort address,
        Func<Exception, Task>? onError = null,
        int retryCount = 0)
    {
        return ExecuteWithRetryAsync(master, async () =>
        {
            var result = await master.ReadCoilsAsync(master.SlaveId, address, 1);
            var newValue = !result[0];
            await master.WriteSingleCoilAsync(master.SlaveId, address, newValue);
            return newValue;
        }, retryCount, onError, $"ToggleCoil({address})");
    }

    #endregion
}
