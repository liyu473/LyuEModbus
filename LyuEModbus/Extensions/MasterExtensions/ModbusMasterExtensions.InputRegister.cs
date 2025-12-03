using LyuEModbus.Abstractions;

namespace LyuEModbus.Extensions;

/// <summary>
/// 输入寄存器（Input Register）读取扩展方法 - 功能码 04（只读）
/// </summary>
public static partial class ModbusMasterExtensions
{
    #region 输入寄存器读取

    /// <summary>
    /// 读取单个输入寄存器
    /// </summary>
    public static Task<ushort?> ReadInputRegisterAsync(
        this IModbusMasterClient master,
        ushort address,
        Func<Exception, Task>? onError = null,
        int retryCount = 0)
    {
        return ExecuteWithRetryAsync(master, async () =>
        {
            var result = await master.ReadInputRegistersAsync(master.SlaveId, address, 1);
            return result[0];
        }, retryCount, onError, $"ReadInputRegister({address})");
    }

    /// <summary>
    /// 批量读取输入寄存器并返回数组
    /// </summary>
    public static Task<ushort[]?> ReadInputRegistersAsync(
        this IModbusMasterClient master,
        ushort startAddress,
        ushort count,
        Func<Exception, Task>? onError = null,
        int retryCount = 0)
    {
        return ExecuteWithRetryRefAsync(master, async () =>
        {
            return await master.ReadInputRegistersAsync(master.SlaveId, startAddress, count);
        }, retryCount, onError, $"ReadInputRegisters({startAddress}, {count})");
    }

    /// <summary>
    /// 批量读取输入寄存器并返回字典
    /// </summary>
    public static Task<Dictionary<ushort, ushort>?> ReadInputRegistersToDictAsync(
        this IModbusMasterClient master,
        ushort startAddress,
        ushort count,
        Func<Exception, Task>? onError = null,
        int retryCount = 0)
    {
        return ExecuteWithRetryRefAsync(master, async () =>
        {
            var result = await master.ReadInputRegistersAsync(master.SlaveId, startAddress, count);
            var dict = new Dictionary<ushort, ushort>();
            for (int i = 0; i < result.Length; i++)
                dict[(ushort)(startAddress + i)] = result[i];
            return dict;
        }, retryCount, onError, $"ReadInputRegistersToDict({startAddress}, {count})",
        d => string.Join(", ", d.Select(kv => $"{kv.Key}={kv.Value}")));
    }

    #endregion
}
