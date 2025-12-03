using LyuEModbus.Abstractions;
using LyuEModbus.Models;

namespace LyuEModbus.Extensions;

/// <summary>
/// IModbusMasterClient 读写扩展方法
/// </summary>
public static class ModbusMasterExtensions
{
    /// <summary>
    /// 默认重试间隔（毫秒）
    /// </summary>
    private const int DefaultRetryDelayMs = 100;

    #region 内部重试策略实现

    /// <summary>
    /// 带重试的执行方法
    /// </summary>
    private static async Task<T?> ExecuteWithRetryAsync<T>(
        IModbusClient client,
        Func<Task<T>> action,
        int retryCount,
        Func<Exception, Task>? onError,
        string operationName) where T : struct
    {
        var attempts = 0;
        while (true)
        {
            try
            {
                var result = await action();
                client.Log(ModbusLogLevel.Debug, $"{operationName} 成功: {result}");
                return result;
            }
            catch (Exception ex)
            {
                attempts++;
                if (attempts > retryCount)
                {
                    client.Log(ModbusLogLevel.Error, $"{operationName} 失败: {ex.Message}");
                    if (onError != null)
                        await onError(ex);
                    return null;
                }
                client.Log(ModbusLogLevel.Warning, $"{operationName} 重试 {attempts}/{retryCount}: {ex.Message}");
                await Task.Delay(DefaultRetryDelayMs);
            }
        }
    }

    /// <summary>
    /// 带重试的执行方法（返回引用类型）
    /// </summary>
    private static async Task<T?> ExecuteWithRetryRefAsync<T>(
        IModbusClient client,
        Func<Task<T>> action,
        int retryCount,
        Func<Exception, Task>? onError,
        string operationName,
        Func<T, string>? formatResult = null) where T : class
    {
        var attempts = 0;
        while (true)
        {
            try
            {
                var result = await action();
                var resultStr = formatResult != null ? formatResult(result) : result.ToString();
                client.Log(ModbusLogLevel.Debug, $"{operationName} 成功: {resultStr}");
                return result;
            }
            catch (Exception ex)
            {
                attempts++;
                if (attempts > retryCount)
                {
                    client.Log(ModbusLogLevel.Error, $"{operationName} 失败: {ex.Message}");
                    if (onError != null)
                        await onError(ex);
                    return null;
                }
                client.Log(ModbusLogLevel.Warning, $"{operationName} 重试 {attempts}/{retryCount}: {ex.Message}");
                await Task.Delay(DefaultRetryDelayMs);
            }
        }
    }

    /// <summary>
    /// 带重试的执行方法（返回 bool 表示成功/失败）
    /// </summary>
    private static async Task<bool> ExecuteWithRetryBoolAsync(
        IModbusClient client,
        Func<Task> action,
        int retryCount,
        Func<Exception, Task>? onError,
        string operationName)
    {
        var attempts = 0;
        while (true)
        {
            try
            {
                await action();
                client.Log(ModbusLogLevel.Debug, $"{operationName} 成功");
                return true;
            }
            catch (Exception ex)
            {
                attempts++;
                if (attempts > retryCount)
                {
                    client.Log(ModbusLogLevel.Error, $"{operationName} 失败: {ex.Message}");
                    if (onError != null)
                        await onError(ex);
                    return false;
                }
                client.Log(ModbusLogLevel.Warning, $"{operationName} 重试 {attempts}/{retryCount}: {ex.Message}");
                await Task.Delay(DefaultRetryDelayMs);
            }
        }
    }

    #endregion

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
        }, retryCount, onError, $"ReadCoils({startAddress}, {count})",
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
        }, retryCount, onError, $"ReadHoldingRegisters({startAddress}, {count})",
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
        }, retryCount, onError, $"ReadInputRegisters({startAddress}, {count})",
        d => string.Join(", ", d.Select(kv => $"{kv.Key}={kv.Value}")));
    }

    #endregion

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
        }, retryCount, onError, $"ReadDiscreteInputs({startAddress}, {count})",
        d => string.Join(", ", d.Select(kv => $"{kv.Key}={kv.Value}")));
    }

    #endregion
}
