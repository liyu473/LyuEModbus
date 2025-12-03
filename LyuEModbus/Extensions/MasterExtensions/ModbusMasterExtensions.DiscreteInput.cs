using LyuEModbus.Abstractions;

namespace LyuEModbus.Extensions;

/// <summary>
/// 离散输入（Discrete Input）读取扩展方法 - 功能码 02（只读）
/// </summary>
public static partial class ModbusMasterExtensions
{
    #region 离散输入读取

    /// <summary>
    /// 读取单个离散输入
    /// </summary>
    public static Task<bool?> ReadDiscreteInputAsync(
        this IModbusMasterClient master,
        ushort address,
        Func<Exception, Task>? onError = null,
        int retryCount = 0)
    {
        return ExecuteWithRetryAsync(master, async () =>
        {
            var result = await master.ReadInputsAsync(master.SlaveId, address, 1);
            return result[0];
        }, retryCount, onError, $"ReadDiscreteInput({address})");
    }

    /// <summary>
    /// 批量读取离散输入并返回数组
    /// </summary>
    public static Task<bool[]?> ReadDiscreteInputsAsync(
        this IModbusMasterClient master,
        ushort startAddress,
        ushort count,
        Func<Exception, Task>? onError = null,
        int retryCount = 0)
    {
        return ExecuteWithRetryRefAsync(master, async () =>
        {
            return await master.ReadInputsAsync(master.SlaveId, startAddress, count);
        }, retryCount, onError, $"ReadDiscreteInputs({startAddress}, {count})");
    }

    /// <summary>
    /// 批量读取离散输入并返回字典
    /// </summary>
    public static Task<Dictionary<ushort, bool>?> ReadDiscreteInputsToDictAsync(
        this IModbusMasterClient master,
        ushort startAddress,
        ushort count,
        Func<Exception, Task>? onError = null,
        int retryCount = 0)
    {
        return ExecuteWithRetryRefAsync(master, async () =>
        {
            var result = await master.ReadInputsAsync(master.SlaveId, startAddress, count);
            var dict = new Dictionary<ushort, bool>();
            for (int i = 0; i < result.Length; i++)
                dict[(ushort)(startAddress + i)] = result[i];
            return dict;
        }, retryCount, onError, $"ReadDiscreteInputsToDict({startAddress}, {count})",
        d => string.Join(", ", d.Select(kv => $"{kv.Key}={kv.Value}")));
    }

    #endregion
}
